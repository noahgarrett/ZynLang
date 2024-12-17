namespace ZynLang.AST.Statements;

public class ForStatementNode(LetStatementNode varDeclaration, ExpressionNode condition, ExpressionNode action, BlockStatementNode body) : StatementNode
{
    public LetStatementNode VarDeclaration { get; set; } = varDeclaration;
    public ExpressionNode Condition { get; set; } = condition;
    public ExpressionNode Action { get; set; } = action;
    public BlockStatementNode Body { get; set; } = body;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "VarDeclaration", VarDeclaration.Json() },
            { "Condition", Condition.Json() },
            { "Action", Action.Json() },
            { "body", Body.Json() },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.ForStatement;
    }
}
