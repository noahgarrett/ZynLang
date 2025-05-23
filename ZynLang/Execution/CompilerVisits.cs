﻿using LLVMSharp.Interop;
using System.Runtime.InteropServices;
using ZynLang.AST;
using ZynLang.AST.Expressions;
using ZynLang.AST.Helpers;
using ZynLang.AST.Literals;
using ZynLang.AST.Statements;

namespace ZynLang.Execution;

public partial class Compiler
{
    private void VisitProgram(ProgramNode node)
    {
        foreach (StatementNode stmt in node.Statements)
            Compile(stmt);

        Console.WriteLine(_module.PrintToString());
    }

    #region Statement Visit Methods
    private void VisitExpressionStatement(ExpressionStatementNode node)
    {
        Compile(node.Expression);
    }

    private void VisitLetStatement(LetStatementNode node)
    {
        string name = node.Name.Value;
        ExpressionNode nodeValue = node.Value;
        string valueType = node.ValueType;

        var (value, type) = ResolveValue(nodeValue, valueType);

        // Verify the value's type matches what was indicated
        //if (GetReturnType(type) != _typeMap[valueType])
        //{
        //    Console.WriteLine($"Compiler: Value in variable declaration ({name}) does not match type declared. Want = {valueType}, Got = {type}");
        //    return;
        //}

        if (_env.Lookup(name) == null)
        {
            LLVMValueRef ptr;

            if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
                ptr = value;
            else
            {
                ptr = _builder.BuildAlloca(GetReturnType(type));
                _builder.BuildStore(value, ptr);
            }

            _env.Define(name, ptr, type);
        }
        else
        {
            var lookupResult = _env.Lookup(name);
            if (lookupResult is (LLVMValueRef vPtr, LLVMTypeRef vType))
                _builder.BuildStore(value, vPtr);
        }
    }

    private void VisitBlockStatement(BlockStatementNode node)
    {
        foreach (StatementNode statement in node.Statements)
            Compile(statement);
    }

    private void VisitReturnStatement(ReturnStatementNode node)
    {
        ExpressionNode rValue = node.ReturnValue;
        var (value, type) = ResolveValue(rValue);

        _builder.BuildRet(value);
        _blockTerminators[_builder.InsertBlock] = true;
    }

    private void VisitFunctionStatement(FunctionStatementNode node)
    {
        string name = node.Name.Value;
        BlockStatementNode body = node.Body;
        List<FunctionParameterNode> parameters = node.Parameters;

        List<string> paramNames = [];
        foreach (var param in parameters)
            paramNames.Add(param.Name);

        List<LLVMTypeRef> paramTypes = [];
        foreach (var param in parameters)
            paramTypes.Add(_typeMap[param.ValueType]);

        LLVMTypeRef returnType = _typeMap[node.ReturnType];

        // https://github.com/noahgarrett/LimeLang/blob/master/Compiler.py#L185
        // https://github.com/davidelettieri/Kaleidoscope/blob/main/Kaleidoscope.Chapter7/Interpreter.cs

        LLVMValueRef f = _module.GetNamedFunction(name);

        // TODO: Explain this or modify/remove
        if (f.Handle != IntPtr.Zero)
        {
            if (f.BasicBlocksCount != 0)
                throw new InvalidOperationException("Redefinition of function.");
            if (f.ParamsCount != parameters.Count)
                throw new InvalidOperationException("Redefinition of function with a different amount of args");
        }

        LLVMTypeRef functionType = LLVMTypeRef.CreateFunction(returnType, paramTypes.ToArray());

        f = _module.AddFunction(name, functionType);
        f.Linkage = LLVMLinkage.LLVMExternalLinkage;

        if (name == "main")
            _mainFunction = f;

        var previousFunction = _currentFunction;
        _currentFunction = f;

        var previousBlock = _builder.InsertBlock;
        var block = f.AppendBasicBlock($"{name}_entry");
        _builder.PositionAtEnd(block);

        var previousEnv = _env;
        _env = new(parent: previousEnv, name: $"{name}_env");

        // Loop through all parameters and generate store instructions in the function block
        for (int i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];

            var param = f.GetParam((uint)i);
            param.Name = p.Name;

            var paramPtr = _builder.BuildAlloca(paramTypes[i], p.Name);
            _builder.BuildStore(param, paramPtr);

            _env.Define(p.Name, paramPtr, paramTypes[i]);
        }

        _env.Define(name, f, returnType);

        Compile(body);

        if (node.ReturnType == "void")
            _builder.BuildRetVoid();

        _currentFunction = previousFunction;

        _env = previousEnv;
        _env.Define(name, f, functionType);

        _builder.PositionAtEnd(previousBlock);
        // TODO: Maybe delete or move somewhere else
        //_passManager.RunFunctionPassManager(f);
    }

    private void VisitAssignStatement(AssignStatementNode node)
    {
        string name = node.Identifier.Value;
        string op = node.Operator;
        ExpressionNode rightNode = node.RightValue;

        if (_env.Lookup(name) == null)
        {
            Errors.Add($"Compiler: Identifier {name} has not been declared before it was re-assigned");
            return;
        }

        var (rightValue, rightType) = ResolveValue(rightNode);

        var lookupResult = _env.Lookup(name);
        if (!lookupResult.HasValue)
        {
            Errors.Add($"Compiler: Unable to lookup existing variable for re-assignment with name: {name}");
            return;
        }

        var (vPtr, vType) = lookupResult.Value;
        LLVMValueRef origValue = _builder.BuildLoad2(vType, vPtr);


        LLVMValueRef value = origValue;
        switch (op)
        {
            case "=":
                value = rightValue;
                break;
            case "+=":
                if (vType == LLVMTypeRef.Int32 && rightType == LLVMTypeRef.Int32)
                    value = _builder.BuildAdd(origValue, rightValue);
                else
                    value = _builder.BuildFAdd(origValue, rightValue);
                break;
            case "-=":
                if (vType == LLVMTypeRef.Int32 && rightType == LLVMTypeRef.Int32)
                    value = _builder.BuildSub(origValue, rightValue);
                else
                    value = _builder.BuildFSub(origValue, rightValue);
                break;
            case "*=":
                if (vType == LLVMTypeRef.Int32 && rightType == LLVMTypeRef.Int32)
                    value = _builder.BuildMul(origValue, rightValue);
                else
                    value = _builder.BuildFMul(origValue, rightValue);
                break;
            case "/=":
                if (vType == LLVMTypeRef.Int32 && rightType == LLVMTypeRef.Int32)
                    value = _builder.BuildSDiv(origValue, rightValue);
                else
                    value = _builder.BuildFDiv(origValue, rightValue);
                break;
            case "_":
                Errors.Add($"Compiler: Unsupported assignment operator ({op})");
                return;
        }

        _builder.BuildStore(value, vPtr);
    }

    private void VisitIfStatement(IfStatementNode node)
    {
        ExpressionNode condition = node.Condition;
        BlockStatementNode consequence = node.Consequence;
        BlockStatementNode? alternative = node.Alternative;

        var (testValue, testType) = ResolveValue(condition);

        // Create the basic blocks for the 'then', 'else', and 'merge' parts
        LLVMBasicBlockRef thenBlock = _currentFunction.AppendBasicBlock($"then_{IncrementCounter()}");
        _blockTerminators[thenBlock] = false;

        LLVMBasicBlockRef elseBlock = null;
        if (alternative != null)
        {
            elseBlock = _currentFunction.AppendBasicBlock($"else_{counter}");
            _blockTerminators[elseBlock] = false;
        }

        LLVMBasicBlockRef mergeBlock = _currentFunction.AppendBasicBlock($"merge_{counter}");
        _blockTerminators[mergeBlock] = false;

        // Create the conditional branch based on the condition
        _builder.BuildCondBr(testValue, thenBlock, alternative == null ? mergeBlock : elseBlock);

        _builder.PositionAtEnd(thenBlock);
        Compile(consequence);
        if (!_blockTerminators[thenBlock])
        {
            _builder.BuildBr(mergeBlock);
            _blockTerminators[thenBlock] = true;
        }

        if (alternative != null && elseBlock != null)
        {
            _builder.PositionAtEnd(elseBlock);
            Compile(alternative);
            if (!_blockTerminators[elseBlock])
            {
                _builder.BuildBr(mergeBlock);
                _blockTerminators[elseBlock] = true;
            }
        }

        _builder.PositionAtEnd(mergeBlock);
    }

    private void VisitWhileStatement(WhileStatementNode node)
    {
        ExpressionNode condition = node.Condition;
        BlockStatementNode body = node.Body;

        var (testValue, testType) = ResolveValue(condition);

        LLVMBasicBlockRef whileEntryBlock = _currentFunction.AppendBasicBlock($"while_entry_{IncrementCounter()}");
        LLVMBasicBlockRef whileOtherwiseBlock = _currentFunction.AppendBasicBlock($"while_otherwise_{counter}");

        Breakpoints.Add(whileOtherwiseBlock);
        Continues.Add(whileEntryBlock);

        _builder.BuildCondBr(testValue, whileEntryBlock, whileOtherwiseBlock);

        _builder.PositionAtEnd(whileEntryBlock);

        Compile(body);

        var (iterationValue, iterationVType) = ResolveValue(condition);
        _builder.BuildCondBr(iterationValue, whileEntryBlock, whileOtherwiseBlock);

        _builder.PositionAtEnd(whileOtherwiseBlock);

        Breakpoints.RemoveAt(Breakpoints.Count - 1);
        Continues.RemoveAt(Continues.Count - 1);
    }
    private void VisitBreakStatement(BreakStatementNode node)
    {
        _builder.BuildBr(Breakpoints[Breakpoints.Count - 1]);
    }

    private void VisitContinueStatement(ContinueStatementNode node)
    {
        _builder.BuildBr(Continues[Continues.Count - 1]);
    }

    private void VisitForStatement(ForStatementNode node)
    {
        LetStatementNode varDeclaration = node.VarDeclaration;
        ExpressionNode condition = node.Condition;
        ExpressionNode action = node.Action;
        BlockStatementNode body = node.Body;

        var idCounter = IncrementCounter();

        var previousEnv = _env;
        _env = new(parent: previousEnv, name: $"for_env_{idCounter}");

        Compile(varDeclaration);

        LLVMBasicBlockRef forEntryBlock = _currentFunction.AppendBasicBlock($"for_entry_{idCounter}");
        LLVMBasicBlockRef forOtherwiseBlock = _currentFunction.AppendBasicBlock($"for_otherwise_{idCounter}");

        Breakpoints.Add(forOtherwiseBlock);
        Continues.Add(forEntryBlock);

        _builder.BuildBr(forEntryBlock);
        _builder.PositionAtEnd(forEntryBlock);

        Compile(body);
        Compile(action);

        var (testValue, testType) = ResolveValue(condition);

        _builder.BuildCondBr(testValue, forEntryBlock, forOtherwiseBlock);
        _builder.PositionAtEnd(forOtherwiseBlock);

        Breakpoints.RemoveAt(Breakpoints.Count - 1);
        Continues.RemoveAt(Continues.Count - 1);
    }

    private void VisitImportStatement(ImportStatementNode node)
    {
        string filePath = node.FilePath;

        switch (filePath)
        {
            case "std":
                // TEMP: Link STD Module
                string stdPath = "C:\\Users\\ngarrett\\Documents\\Other\\ZynLang\\ZynLang\\BuiltIns\\std\\math.ll";

                // Load the std module
                LLVMModuleRef stdModule = LoadModule(stdPath);
                bool linkResult = LinkModules(_module, stdModule);
                if (!linkResult)
                {
                    throw new Exception("Error linking modules");
                }

                return;
        }

        //string fileContent = File.ReadAllText($"C:\\Users\\noahw\\OneDrive\\Desktop\\Blank Software, LLC\\Github\\ZynLang\\ZynLang\\Test\\pallets\\{filePath}");
        string fileContent = File.ReadAllText($"C:\\Users\\ngarrett\\Documents\\Other\\ZynLang\\ZynLang\\Test\\pallets\\{filePath}");

        Lexer lexer = new(fileContent);
        Parser parser = new(lexer);

        ProgramNode programNode = parser.ParseProgram();
        if (parser.Errors.Count > 0)
        {
            foreach (var error in parser.Errors)
                Console.WriteLine($"Parser Error: {error}");
        }

        VisitProgram(programNode);
    }

    private void VisitImportFromStatement(ImportFromStatementNode node)
    {
        string palletName = node.PalletName.Value;
        string filePath = $"pallets\\{node.PalletName.Value}.lime";

        // TODO: Implement using pre-parsed pallets

        string fileContent = File.ReadAllText($"C:\\Users\\ngarrett\\Documents\\Other\\ZynLang\\ZynLang\\Test\\{filePath}.lime");

        Lexer lexer = new(fileContent);
        Parser parser = new(lexer);

        ProgramNode programNode = parser.ParseProgram();
        if (parser.Errors.Count > 0)
        {
            foreach (var error in parser.Errors)
                Console.WriteLine($"Parser Error with imported pallet - {filePath} -: {error}");
        }

        foreach (var export in programNode.Exports)
        {
            foreach (var ident in node.Imports)
            {
                if (export is FunctionStatementNode fEX)
                {
                    if (ident.Value == fEX.Name.Value)
                        Compile(fEX);
                }
                else if (export is LetStatementNode lEX)
                {
                    if (ident.Value == lEX.Name.Value)
                        Compile(lEX);
                }
            }
        }
    }
    #endregion

    #region Expression Visit Methods
    private (LLVMValueRef, LLVMTypeRef) VisitInfixExpression(InfixExpressionNode node)
    {
        string op = node.Operator;
        var (leftValue, leftType) = ResolveValue(node.LeftNode);
        var (rightValue, rightType) = ResolveValue(node.RightNode);

        var lType = GetReturnType(leftType);
        var rType = GetReturnType(rightType);

        // Integers
        if (rType == LLVMTypeRef.Int32 && lType == LLVMTypeRef.Int32)
        {
            return op switch
            {
                // Arithmetic
                "+" => (_builder.BuildAdd(leftValue, rightValue), LLVMTypeRef.Int32),
                "-" => (_builder.BuildSub(leftValue, rightValue), LLVMTypeRef.Int32),
                "*" => (_builder.BuildMul(leftValue, rightValue), LLVMTypeRef.Int32),
                "/" => (_builder.BuildSDiv(leftValue, rightValue), LLVMTypeRef.Int32),
                "%" => (_builder.BuildSRem(leftValue, rightValue), LLVMTypeRef.Int32),

                // Comparison
                "<" => (_builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, leftValue, rightValue), LLVMTypeRef.Int1),
                "<=" => (_builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, leftValue, rightValue), LLVMTypeRef.Int1),
                ">" => (_builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, leftValue, rightValue), LLVMTypeRef.Int1),
                ">=" => (_builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, leftValue, rightValue), LLVMTypeRef.Int1),
                "==" => (_builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, leftValue, rightValue), LLVMTypeRef.Int1),

                _ => (leftValue, LLVMTypeRef.Int32),
            };
        }
        // Floats/Doubles
        else if (rType == LLVMTypeRef.Double && lType == LLVMTypeRef.Double)
        {
            return op switch
            {
                // Arithmetic
                "+" => (_builder.BuildFAdd(leftValue, rightValue), LLVMTypeRef.Double),
                "-" => (_builder.BuildFSub(leftValue, rightValue), LLVMTypeRef.Double),
                "*" => (_builder.BuildFMul(leftValue, rightValue), LLVMTypeRef.Double),
                "/" => (_builder.BuildFDiv(leftValue, rightValue), LLVMTypeRef.Double),
                "%" => (_builder.BuildFRem(leftValue, rightValue), LLVMTypeRef.Double),

                // Comparison
                "<" => (_builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, leftValue, rightValue), LLVMTypeRef.Int1),
                "<=" => (_builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, leftValue, rightValue), LLVMTypeRef.Int1),
                ">" => (_builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, leftValue, rightValue), LLVMTypeRef.Int1),
                ">=" => (_builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, leftValue, rightValue), LLVMTypeRef.Int1),
                "==" => (_builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, leftValue, rightValue), LLVMTypeRef.Int1),

                _ => (leftValue, LLVMTypeRef.Double),
            };
        }
        // TODO: Strings
        else if (rType.Kind == LLVMTypeKind.LLVMPointerTypeKind && lType.Kind == LLVMTypeKind.LLVMPointerTypeKind && IsStringType(rType) && IsStringType(lType))
        {
            Console.WriteLine("stop tryna do this dumbass shit big dog");
        }

        Console.WriteLine("big error in visit infix expression big dog");
        return (leftValue, LLVMTypeRef.Int32);
    }

    private (LLVMValueRef, LLVMTypeRef) VisitCallExpression(CallExpressionNode node)
    {
        string name = node.FunctionName.Value;
        List<ExpressionNode> args = node.Arguments;

        List<LLVMValueRef> argValues = [];
        List<LLVMTypeRef> argTypes = [];
        if (args.Count > 0)
        {
            foreach (ExpressionNode x in args)
            {
                var (aVal, aType) = ResolveValue(x);
                argValues.Add(aVal);
                argTypes.Add(aType);
            }
        }

        var existingFunction = _module.GetNamedFunction(name);

        if (existingFunction.Handle != IntPtr.Zero)
        {
            if (args.Count != existingFunction.ParamsCount)
            {
                throw new Exception($"Incorrect number of arguments passed. Want: {existingFunction.ParamsCount} | Got: {args.Count}");
            }

            unsafe
            {
                LLVMTypeRef funcType = LLVM.TypeOf(existingFunction);
                return (_builder.BuildCall2(funcType, existingFunction, [.. argValues]), funcType);
            }
        }

        switch (name)
        {
            default:
                var lookupResult = _env.Lookup(name);
                if (lookupResult is (LLVMValueRef vPtr, LLVMTypeRef vType))
                {
                    return (_builder.BuildCall2(vType, vPtr, [.. argValues]), vType);
                }
                return (null, null); // should be unreachable
        }
    }

    private (LLVMValueRef, LLVMTypeRef) VisitPrefixExpression(PrefixExpressionNode node)
    {
        string op = node.Operator;
        ExpressionNode rightNode = node.RightNode;

        var (rightValue, rightType) = ResolveValue(rightNode);

        LLVMTypeRef valueType = null;
        LLVMValueRef value = null;
        if (rightType == LLVMTypeRef.Int32)
        {
            valueType = LLVMTypeRef.Int32;
            switch (op)
            {
                case "-":
                    value = _builder.BuildMul(rightValue, LLVMValueRef.CreateConstInt(valueType, ulong.MaxValue, true));
                    break;
                case "!":
                    value = _builder.BuildNot(rightValue);
                    break;
                default:
                    throw new Exception($"Unsupported operation '{op}' for type Double");
            }
        }
        else if (rightType == LLVMTypeRef.Double)
        {
            valueType = LLVMTypeRef.Double;
            switch (op)
            {
                case "-":
                    value = _builder.BuildFNeg(rightValue);
                    break;
                case "!":
                    LLVMValueRef zeroValue = LLVMValueRef.CreateConstReal(valueType, 0.0);
                    value = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, rightValue, zeroValue);
                    break;
                default:
                    throw new Exception($"Unsupported operation '{op}' for type Double");
            }
        }

        return (value, valueType);
    }

    private void VisitPostfixExpression(PostfixExpressionNode node)
    {
        IdentifierLiteralNode leftNode = (IdentifierLiteralNode)node.RightNode;
        string op = node.Operator;

        var result = _env.Lookup(leftNode.Value);
        if (result == null)
        {
            Errors.Add($"COMPILE ERROR: Identifier {leftNode.Value} has not been declared before it was used in a PostfixExpression.");
            return;
        }

        var (varPtr, varType) = ((LLVMValueRef, LLVMTypeRef))result;
        LLVMValueRef origValue = _builder.BuildLoad2(varType, varPtr);

        LLVMValueRef value = null;
        switch (op)
        {
            case "++":
                if (origValue.TypeOf == LLVMTypeRef.Int32)
                    value = _builder.BuildAdd(origValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1));
                break;
            case "--":
                if (origValue.TypeOf == LLVMTypeRef.Int32)
                    value = _builder.BuildSub(origValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1));
                break;
        }

        _builder.BuildStore(value, varPtr);
    }

    private (LLVMValueRef, LLVMTypeRef) VisitIndexExpression(IndexExpressionNode node)
    {
        //ExpressionNode leftNode = node.LeftNode;
        //ExpressionNode indexNode = node.IndexNode;

        //var (leftValue, leftType) = ResolveValue(leftNode);
        //var (indexValue, indexType) = ResolveValue(indexNode);

        //LLVMValueRef elementPtr = _builder.BuildInBoundsGEP2(
        //    leftType,
        //    leftValue,
        //    [
        //        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false), // Base offset
        //        indexValue // Index
        //    ]
        //);

        //LLVMValueRef elementValue = _builder.BuildLoad2(leftType.ElementType, elementPtr);

        //return (elementValue, indexType);
        ExpressionNode leftNode = node.LeftNode;
        ExpressionNode indexNode = node.IndexNode;

        // Resolve the container and index
        var (containerValue, containerType) = ResolveValue(leftNode);
        var (indexValue, indexType) = ResolveValue(indexNode);

        // Check if the container is an array
        if (containerType.Kind == LLVMTypeKind.LLVMArrayTypeKind)
        {
            // Handle array indexing
            LLVMValueRef elementPtr = _builder.BuildInBoundsGEP2(
                containerType,
                containerValue,
                new LLVMValueRef[]
                {
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false), // Base offset
                indexValue // Integer index
                }
            );

            LLVMValueRef elementValue = _builder.BuildLoad2(containerType.ElementType, elementPtr);
            return (elementValue, containerType.ElementType);
        }

        Console.WriteLine("=== INDEX FUNCTION WAS NOT ARRAY ===");
        return (null, null);
    }
    #endregion

    
}