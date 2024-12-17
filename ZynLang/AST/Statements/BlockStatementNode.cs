using Newtonsoft.Json;
using System.Dynamic;

namespace ZynLang.AST.Statements;

public class BlockStatementNode(List<StatementNode> statements) : StatementNode
{
    public List<StatementNode> Statements { get; set; } = statements;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Statements", Statements.ConvertAll(stmt => stmt.Json()) }
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.BlockStatement;
    }
}
