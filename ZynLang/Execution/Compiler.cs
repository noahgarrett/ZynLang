using LLVMSharp.Interop;
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

    private Context _env;

    private Dictionary<string, LLVMTypeRef> _typeMap;

    public Compiler()
    {
        _typeMap = new()
        {
            {"int", LLVMTypeRef.Int32 },
            {"float", LLVMTypeRef.Float },
            {"bool", LLVMTypeRef.Int1 },
            {"void", LLVMTypeRef.Void },
            {"str", LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 8) }
        };

        _env = new();

        LLVM.LinkInMCJIT();
        LLVM.InitializeX86TargetMC();
        LLVM.InitializeX86Target();
        LLVM.InitializeX86TargetInfo();
        LLVM.InitializeX86AsmParser();
        LLVM.InitializeX86AsmPrinter();

        InitializeModule();
    }

    public void Run(ProgramNode node)
    {
        VisitProgram(node);
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

            // Expressions
            case NodeType.InfixExpression:
                VisitInfixExpression((InfixExpressionNode)node);
                break;
        }
    }

    private void InitializeModule()
    {
        _module = LLVMModuleRef.CreateWithName("ZynLang Module");
        _builder = _module.Context.CreateBuilder();

        // Initialize all optimization passes
        _passManager = _module.CreateFunctionPassManager();
        _passManager.AddBasicAliasAnalysisPass();
        _passManager.AddPromoteMemoryToRegisterPass();
        _passManager.AddInstructionCombiningPass();
        _passManager.AddReassociatePass();
        _passManager.AddGVNPass();
        _passManager.AddCFGSimplificationPass();
        _passManager.InitializeFunctionPassManager();

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

    }

    private void VisitBlockStatement(BlockStatementNode node)
    {
        foreach (StatementNode statement in node.Statements)
            Compile(statement);
    }

    private void VisitReturnStatement(ReturnStatementNode node)
    {
        Console.WriteLine("HITTTT");
        ExpressionNode rValue = node.ReturnValue;
        var (value, type) = ResolveValue(rValue);

        _builder.BuildRet(value);
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

        LLVMTypeRef function = LLVMTypeRef.CreateFunction(returnType, paramTypes.ToArray());

        f = _module.AddFunction(name, function);
        f.Linkage = LLVMLinkage.LLVMExternalLinkage;

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

        _env = previousEnv;
        _env.Define(name, f, returnType);

        // TODO: Maybe delete or move somewhere else
        //_passManager.RunFunctionPassManager(f);
    }

    private void VisitAssignStatement(AssignStatementNode node)
    {

    }

    private void VisitIfStatement(IfStatementNode node)
    {

    }

    private void VisitWhileStatement(WhileStatementNode node)
    {

    }
    private void VisitBreakStatement(BreakStatementNode node)
    {

    }

    private void VisitContinueStatement(ContinueStatementNode node)
    {

    }

    private void VisitForStatement(ForStatementNode node)
    {

    }

    private void VisitImportStatement(ImportStatementNode node)
    {

    }
    #endregion

    #region Expression Visit Methods
    private (LLVMValueRef, LLVMTypeRef) VisitInfixExpression(InfixExpressionNode node)
    {
        string op = node.Operator;
        var (leftValue, leftType) = ResolveValue(node);
        var (rightValue, rightType) = ResolveValue(node);

        if (rightType.ElementType == LLVMTypeRef.Int32 && leftType.ElementType == LLVMTypeRef.Int32)
        {
            return op switch
            {
                "+" => (_builder.BuildAdd(leftValue, rightValue), LLVMTypeRef.Int32),
                "-" => (_builder.BuildSub(leftValue, rightValue), LLVMTypeRef.Int32),
                "*" => (_builder.BuildMul(leftValue, rightValue), LLVMTypeRef.Int32),
                "/" => (_builder.BuildSDiv(leftValue, rightValue), LLVMTypeRef.Int32),
                _ => (leftValue, LLVMTypeRef.Int32),
            };
        }

        Console.WriteLine("big error in visit infix expression big dog");
        return (leftValue, LLVMTypeRef.Int32);
    }

    private void VisitCallExpression(CallExpressionNode node)
    {

    }

    private void VisitPrefixExpression(PrefixExpressionNode node)
    {

    }
    private void VisitPostfixExpression(PostfixExpressionNode node)
    {
        
    }
    #endregion

    #region Helper Visit Methods
    private (LLVMValueRef, LLVMTypeRef) ResolveValue(ExpressionNode node)
    {
        switch (node.Type())
        {
            case NodeType.IntegerLiteral:
                IntegerLiteralNode _node = (IntegerLiteralNode)node;
                return (LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)_node.Value), LLVMTypeRef.Int32);
            default:
                Console.WriteLine("RESOLVE VALUE WENT TO DEFAULT WTF DUDE");
                return (LLVMValueRef.CreateConstReal(LLVMTypeRef.Int32, 69), LLVMTypeRef.Int32);
        }
    }

    /*private (LLVMValueRef, LLVMTypeRef) ConvertString(string value)
    {
        
    }*/
    #endregion
}
