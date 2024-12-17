using System.Dynamic;
using System.Text.Json.Nodes;
using Newtonsoft.Json;

namespace ZynLang.AST;

public class ProgramNode : Node
{
    public List<StatementNode> Statements { get; set; } = [];
    public List<StatementNode> Exports { get; set; } = [];

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Statements", Statements.ConvertAll(stmt => stmt.Json()) },
            { "Exports", Exports.ConvertAll(export => export.Json()) }
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.Program;
    }
}
