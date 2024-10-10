using LLVMSharp.Interop;
using ZynLang.AST;
using ZynLang.AST.Expressions;
using ZynLang.AST.Literals;
using ZynLang.AST.Statements;

namespace ZynLang.Execution;

public class Compiler
{
    private LLVMModuleRef _module;
    private LLVMBuilderRef _builder;
    private LLVMPassManagerRef _passManager;
    private LLVMExecutionEngineRef _engine;

    public Compiler()
    {
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

    }

    private void VisitReturnStatement(ReturnStatementNode node)
    {

    }

    private void VisitFunctionStatement(FunctionStatementNode node)
    {

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
                return (LLVMValueRef.CreateConstReal(LLVMTypeRef.Int32, _node.Value), LLVMTypeRef.Int32);
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
