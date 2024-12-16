﻿using LLVMSharp.Interop;
using System;
using System.Net.Mail;
using System.Xml.Linq;
using ZynLang.AST;
using ZynLang.AST.Expressions;
using ZynLang.AST.Helpers;
using ZynLang.AST.Literals;
using ZynLang.AST.Statements;
using ZynLang.Models;

namespace ZynLang.Execution;

public class Compiler
{
    private LLVMModuleRef _module;
    private LLVMBuilderRef _builder;
    private LLVMPassManagerRef _passManager;
    private LLVMExecutionEngineRef _engine;

    private LLVMValueRef _mainFunction;
    private LLVMValueRef _currentFunction;

    private Context _env;

    private Dictionary<string, LLVMTypeRef> _typeMap;

    private uint counter = 0;

    private List<LLVMBasicBlockRef> Breakpoints = [];
    private List<LLVMBasicBlockRef> Continues = [];

    private readonly Dictionary<LLVMBasicBlockRef, bool> _blockTerminators = [];

    private readonly string StandardLibraryPath = Path.Combine(AppContext.BaseDirectory, "BuiltIns", "std");

    public List<string> Errors = [];

    public Compiler()
    {
        _typeMap = new()
        {
            {"int", LLVMTypeRef.Int32 },
            {"float", LLVMTypeRef.Double },
            {"bool", LLVMTypeRef.Int1 },
            {"void", LLVMTypeRef.Void },
            {"str", LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
            {"dict", LLVMTypeRef.CreateStruct([
                LLVMTypeRef.CreatePointer(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), 0),
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int32, 0),
                LLVMTypeRef.Int32
            ], false) },    // TODO: Allow values other than integers

            {"arr_int", LLVMTypeRef.Int32 },
            {"arr_float", LLVMTypeRef.Double },
            {"arr_bool", LLVMTypeRef.Int1 },
            {"arr_str", LLVMTypeRef.CreateArray(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), 0) },
        };

        _env = new();

        LLVM.LinkInMCJIT();
        LLVM.InitializeX86TargetMC();
        LLVM.InitializeX86Target();
        LLVM.InitializeX86TargetInfo();
        LLVM.InitializeX86AsmParser();
        LLVM.InitializeX86AsmPrinter();

        InitializeModule();

        SetupBuiltinFunctions();    // Functions pre-built for users

        SetupInternalFunctions();   // Functions used internally for compiler
    }

    public void Run(ProgramNode node)
    {
        VisitProgram(node);
    }

    public void Execute()
    {
        _module.PrintToString();
        _module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        var res = _engine.RunFunction(_mainFunction, Array.Empty<LLVMGenericValueRef>());

        ulong resultAsInt = 2;

        unsafe
        {
            resultAsInt = LLVM.GenericValueToInt(res, 1);
        }
        

        Console.WriteLine("\n> {0}", resultAsInt);

        _builder.Dispose();
        _module.Dispose();
        //_engine.Dispose();
    }

    // Function to extract the return type from a function type
    LLVMTypeRef GetReturnType(LLVMTypeRef type)
    {
        if (type.Kind == LLVMTypeKind.LLVMFunctionTypeKind) // Check if the type is a function type
        {
            return type.ReturnType; // Extract the return type
        }
        else if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
        {
            return type.ElementType;
        }

        return type; // Otherwise, it's already the return type
    }

    private uint IncrementCounter()
    {
        counter++;
        return counter;
    }

    // 1 + 1;
    private void Compile(Node node)
    {
        switch (node.Type())
        {
            case NodeType.Program:
                VisitProgram((ProgramNode)node);
                break;

            // Statements
            case NodeType.ExpressionStatement:
                VisitExpressionStatement((ExpressionStatementNode)node); 
                break;
            case NodeType.LetStatement:
                VisitLetStatement((LetStatementNode)node); 
                break;
            case NodeType.FunctionStatement:
                VisitFunctionStatement((FunctionStatementNode)node);
                break;
            case NodeType.ReturnStatement:
                VisitReturnStatement((ReturnStatementNode)node);
                break;
            case NodeType.BlockStatement:
                VisitBlockStatement((BlockStatementNode)node);
                break;
            case NodeType.IfStatement:
                VisitIfStatement((IfStatementNode)node);
                break;
            case NodeType.AssignStatement:
                VisitAssignStatement((AssignStatementNode)node);
                break;
            case NodeType.WhileStatement:
                VisitWhileStatement((WhileStatementNode)node);
                break;
            case NodeType.ForStatement:
                VisitForStatement((ForStatementNode)node);
                break;
            case NodeType.BreakStatement:
                VisitBreakStatement((BreakStatementNode)node);
                break;
            case NodeType.ContinueStatement:
                VisitContinueStatement((ContinueStatementNode)node);
                break;
            case NodeType.ImportStatement:
                VisitImportStatement((ImportStatementNode)node);
                break;
            case NodeType.ImportFromStatement:
                VisitImportFromStatement((ImportFromStatementNode)node);
                break;

            // Expressions
            case NodeType.InfixExpression:
                VisitInfixExpression((InfixExpressionNode)node);
                break;
            case NodeType.CallExpression:
                VisitCallExpression((CallExpressionNode)node);
                break;
            case NodeType.PostfixExpression:
                VisitPostfixExpression((PostfixExpressionNode)node);
                break;
            case NodeType.IndexExpression:
                VisitIndexExpression((IndexExpressionNode)node);
                break;
        }
    }

    private void InitializeModule()
    {
        _module = LLVMModuleRef.CreateWithName("ZynLang Module");
        _builder = _module.Context.CreateBuilder();

        // Initialize all optimization passes
        /*_passManager = _module.CreateFunctionPassManager();
        _passManager.AddBasicAliasAnalysisPass();
        _passManager.AddPromoteMemoryToRegisterPass();
        _passManager.AddInstructionCombiningPass();
        _passManager.AddReassociatePass();
        _passManager.AddGVNPass();
        _passManager.AddCFGSimplificationPass();
        _passManager.InitializeFunctionPassManager();*/

        _engine = _module.CreateMCJITCompiler();
    }

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

    #region Helper Visit Methods
    private (LLVMValueRef, LLVMTypeRef) ResolveValue(ExpressionNode node, string? valueType = null)
    {
        switch (node.Type())
        {
            case NodeType.IntegerLiteral:
                IntegerLiteralNode _node = (IntegerLiteralNode)node;
                return (LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)_node.Value), LLVMTypeRef.Int32);
            case NodeType.FloatLiteral:
                FloatLiteralNode fNode = (FloatLiteralNode)node;
                return (LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, fNode.Value), LLVMTypeRef.Double);
            case NodeType.StringLiteral:
                StringLiteralNode sNode = (StringLiteralNode)node;

                // Properly handle escape sequences
                string escapedValue = sNode.Value.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");

                // Create a global constant for the string
                LLVMValueRef stringGlobal = _builder.BuildGlobalStringPtr(escapedValue);

                // Return the pointer to the string and its type (i8*)
                return (stringGlobal, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
            case NodeType.ArrayLiteral:
                ArrayLiteralNode aNode = (ArrayLiteralNode)node;

                // TODO: Verify that valueType is not null
                var elementType = _typeMap[valueType];
                uint elementCount = (uint)aNode.Elements.Count;

                LLVMTypeRef arrayType = LLVMTypeRef.CreateArray(elementType, elementCount);
                LLVMValueRef arrayAlloc = _builder.BuildAlloca(arrayType);

                for (int i = 0; i < aNode.Elements.Count; i++)
                {
                    (LLVMValueRef elementValue, LLVMTypeRef resolvedType) = ResolveValue(aNode.Elements[i]);

                    // TODO: Verify the resolved type is the same as the arrays declared type

                    LLVMValueRef elementPtr = _builder.BuildInBoundsGEP2(
                        arrayType,
                        arrayAlloc,
                        [
                            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false),       // Offset to the start of the array
                            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)i, false) // Current element index
                        ]
                    );

                    _builder.BuildStore(elementValue, elementPtr);
                }

                return (arrayAlloc, arrayType);
            case NodeType.HashLiteral:
                HashLiteralNode hNode = (HashLiteralNode)node;

                //LLVMValueRef mallocFn = _module.GetNamedFunction("malloc");
                LLVMValueRef mallocFn = _module.AddFunction(
                    "malloc",
                    LLVMTypeRef.CreateFunction(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), new LLVMTypeRef[] { LLVMTypeRef.Int64 }, false)
                );

                // Allocate memory for dict
                LLVMValueRef dictPtr = _builder.BuildCall2(
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                    mallocFn,
                    new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 64, false) },
                    "dict_ptr"
                );

                LLVMValueRef keysArray = _builder.BuildCall2(
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                    mallocFn,
                    new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 128, false) },
                    "keys_array"
                );

                LLVMValueRef valuesArray = _builder.BuildCall2(
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                    mallocFn,
                    new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 64, false) },
                    "values_array"
                );

                // Store the keys in the dict
                // TODO: allow multiple keys and values
                LLVMValueRef keysArrayPtr = _builder.BuildStructGEP2(
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), // MIGHT BE ISSUE
                    dictPtr,
                    0,
                    "keys_array_ptr"
                );
                _builder.BuildStore(keysArray, keysArrayPtr);

                LLVMValueRef valuesArrayPtr = _builder.BuildStructGEP2(
                    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), // MIGHT BE ISSUE
                    dictPtr,
                    1,
                    "values_array_ptr"
                );
                _builder.BuildStore(valuesArray, valuesArrayPtr);

                LLVMValueRef capacityPtr = _builder.BuildStructGEP2(
                     LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), // MIGHT BE ISSUE
                     dictPtr,
                     2,
                     "capacity_ptr"
                );
                _builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 16, false), capacityPtr);

                // Insert key-value pair
                foreach (var kvp in hNode.Pairs)
                {
                    var (keyVal, keyType) = ResolveValue(kvp.Key);
                    var (valVal, valType) = ResolveValue(kvp.Value);

                    LLVMValueRef hashValue = _builder.BuildCall2(
                        LLVMTypeRef.Int32,
                        _module.GetNamedFunction("internal_hash"),
                        new LLVMValueRef[] { keyVal },
                        "hash_value"
                    );

                    LLVMValueRef capacity = _builder.BuildLoad2(LLVMTypeRef.Int32, capacityPtr, "capacity");
                    LLVMValueRef index = _builder.BuildURem(hashValue, capacity, "index");

                    // Store key in keys array
                    LLVMValueRef keySlot = _builder.BuildGEP2(
                        LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                        keysArray,
                        new LLVMValueRef[] { index },
                        "key_slot"
                    );
                    _builder.BuildStore(keyVal, keySlot);

                    // Store value in values array
                    LLVMValueRef valueSlot = _builder.BuildGEP2(
                        LLVMTypeRef.Int32,
                        valuesArray,
                        new LLVMValueRef[] { index },
                        "value_slot"
                    );
                    _builder.BuildStore(valVal, valueSlot);

                    break; // TODO: make sure this works with multiple key value pairs
                }

                return (dictPtr, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));

            //// Initialize entries into the hash map
            //int index = 0;
            //foreach (var kvp in hNode.Pairs)
            //{
            //    ExpressionNode keyNode = kvp.Key;
            //    ExpressionNode valueNode = kvp.Value;

            //    // Resolve the values for these nodes
            //    var (keyValue, keyTypeResolved) = ResolveValue(keyNode);
            //    var (valueValue, valueTypeResolved) = ResolveValue(valueNode);

            //    // Get the pointer to the current entry
            //    LLVMValueRef entryPtr = _builder.BuildInBoundsGEP2(
            //        entryType,
            //        hashMap,
            //        [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)index++, false)]
            //    );

            //    // Set the key and value in the entry
            //    LLVMValueRef keyPtr = _builder.BuildStructGEP2(entryType, entryPtr, 0);
            //    LLVMValueRef valuePtr = _builder.BuildStructGEP2(entryType, entryPtr, 1);
            //    _builder.BuildStore(keyValue, keyPtr);
            //    _builder.BuildStore(valueValue, valuePtr);
            //}

            //return (hashMap, hashMapType);

            case NodeType.BooleanLiteral:
                BooleanLiteralNode bNode = (BooleanLiteralNode)node;
                int boolConv = bNode.Value ? 1 : 0;
                return (LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, (ulong)boolConv), LLVMTypeRef.Int1);
            case NodeType.IdentifierLiteral:
                IdentifierLiteralNode ident = (IdentifierLiteralNode)node;
                var (ptr, type) = ((LLVMValueRef, LLVMTypeRef))_env.Lookup(ident.Value);

                if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
                {
                    return (ptr, type);
                }

                return (_builder.BuildLoad2(GetReturnType(type), ptr), GetReturnType(type));

            // Expression Values
            case NodeType.InfixExpression:
                return VisitInfixExpression((InfixExpressionNode)node);
            case NodeType.CallExpression:
                return VisitCallExpression((CallExpressionNode)node);
            case NodeType.PrefixExpression:
                return VisitPrefixExpression((PrefixExpressionNode)node);
            case NodeType.IndexExpression:
                return VisitIndexExpression((IndexExpressionNode)node);

            default:
                Console.WriteLine("RESOLVE VALUE WENT TO DEFAULT WTF DUDE");
                return (LLVMValueRef.CreateConstReal(LLVMTypeRef.Int32, 69), LLVMTypeRef.Int32);
        }
    }
    #endregion

    #region Compiler Setup
    private void SetupBuiltinFunctions()
    {
        // Declare printf
        LLVMTypeRef printfType = LLVMTypeRef.CreateFunction(
            LLVMTypeRef.Int32,
            new[] { LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0) },
            IsVarArg: true
        );

        LLVMValueRef printfFunction = _module.AddFunction("printf", printfType);
        _env.Define("print", printfFunction, printfType);
    }

    private void SetupInternalFunctions()
    {
        // Hash function
        SetupInternalHashFunction();

        // Malloc 64-bit
        //LLVMTypeRef mallocType = LLVMTypeRef.CreateFunction(
        //    LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
        //    [LLVMTypeRef.Int64],
        //    false
        //);
        //_module.AddFunction("malloc", mallocType);
    }

    private void SetupInternalHashFunction()
    {
        LLVMTypeRef hashFuncType = LLVMTypeRef.CreateFunction(
            LLVMTypeRef.Int32,
            [LLVMTypeRef.CreatePointer(LLVMTypeRef.Int32, 0)],
            false
        );

        LLVMValueRef hashFunc = _module.AddFunction("internal_hash", hashFuncType);

        LLVMBasicBlockRef entryBlock = hashFunc.AppendBasicBlock("entry");
        _builder.PositionAtEnd(entryBlock);

        // Initialize hash
        LLVMValueRef hash = _builder.BuildAlloca(LLVMTypeRef.Int32, "hash");
        _builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 5381, false), hash);
        LLVMValueRef ptr = hashFunc.GetParam(0); // Input string pointer

        // Jump to the loop block
        LLVMBasicBlockRef loopBlock = hashFunc.AppendBasicBlock("hash_loop");
        _builder.BuildBr(loopBlock);

        // Loop block
        _builder.PositionAtEnd(loopBlock);
        LLVMValueRef currentChar = _builder.BuildLoad2(
            LLVMTypeRef.Int8,
            _builder.BuildGEP2(LLVMTypeRef.Int8, ptr, new LLVMValueRef[] {
        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false)
            }, "char_ptr"),
            "current_char"
        );

        // Check if the character is null (end of string)
        LLVMValueRef isNull = _builder.BuildICmp(
            LLVMIntPredicate.LLVMIntEQ,
            currentChar,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, 0, false),
            "is_null"
        );

        LLVMBasicBlockRef endBlock = hashFunc.AppendBasicBlock("hash_end");
        LLVMBasicBlockRef bodyBlock = hashFunc.AppendBasicBlock("hash_body");

        // Conditional branch based on null check
        _builder.BuildCondBr(isNull, endBlock, bodyBlock);

        // Body block (process character and update hash)
        _builder.PositionAtEnd(bodyBlock);
        LLVMValueRef hashValue = _builder.BuildLoad2(LLVMTypeRef.Int32, hash, "current_hash");
        LLVMValueRef hashUpdated = _builder.BuildAdd(
            _builder.BuildMul(hashValue, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 33, false), "hash_mul"),
            _builder.BuildZExt(currentChar, LLVMTypeRef.Int32, "hash_char_ext"),
            "new_hash"
        );
        _builder.BuildStore(hashUpdated, hash);

        // Advance pointer to the next character
        ptr = _builder.BuildGEP2(
            LLVMTypeRef.Int8,
            ptr,
            new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1, false) },
            "next_char"
        );

        // Loop back
        _builder.BuildBr(loopBlock);

        // End block (return final hash)
        _builder.PositionAtEnd(endBlock);
        LLVMValueRef finalHash = _builder.BuildLoad2(LLVMTypeRef.Int32, hash, "final_hash");
        _builder.BuildRet(finalHash);
    }
    #endregion

    #region Private Helpers
    /// <summary>
    /// Checks to see if a LLVM type is a i8*
    /// </summary>
    /// <param name="rType"></param>
    /// <param name="lType"></param>
    private bool IsStringType(LLVMTypeRef type) => type.Kind == LLVMTypeKind.LLVMIntegerTypeKind && type.IntWidth == 8;
    #endregion
}