namespace ZynLang.AST.Statements;

public class ContinueStatementNode : StatementNode
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
        return NodeType.ContinueStatement;
    }
}
