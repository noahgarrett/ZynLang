namespace ZynLang.AST.Statements;

public class ReturnStatementNode(ExpressionNode returnValue) : Node
{
    public ExpressionNode ReturnValue { get; set; } = returnValue;

    public override NodeType Type()
    {
        return NodeType.ReturnStatement;
    }
}
