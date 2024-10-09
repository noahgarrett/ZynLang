namespace ZynLang.AST.Statements;

public class ReturnStatementNode(ExpressionNode returnValue) : StatementNode
{
    public ExpressionNode ReturnValue { get; set; } = returnValue;

    public override NodeType Type()
    {
        return NodeType.ReturnStatement;
    }
}
