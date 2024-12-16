namespace ZynLang.AST.Statements;

public class ExpressionStatementNode(ExpressionNode expr) : StatementNode
{
    public ExpressionNode Expression { get; set; } = expr;

    public override string Json()
    {
        throw new NotImplementedException();
    }

    public override NodeType Type()
    {
        return NodeType.ExpressionStatement;
    }
}
