namespace ZynLang.AST.Statements;

public class ExpressionStatementNode : Node
{
    public ExpressionNode Expression { get; set; }

    public override NodeType Type()
    {
        return NodeType.ExpressionStatement;
    }
}
