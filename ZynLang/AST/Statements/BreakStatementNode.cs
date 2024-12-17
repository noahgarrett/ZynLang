namespace ZynLang.AST.Statements;

public class BreakStatementNode : StatementNode
{
    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.BreakStatement;
    }
}
