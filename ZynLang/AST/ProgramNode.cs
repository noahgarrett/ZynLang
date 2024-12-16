using System.Dynamic;
using Newtonsoft.Json;

namespace ZynLang.AST;

public class ProgramNode : Node
{
    public List<StatementNode> Statements { get; set; } = [];
    public List<StatementNode> Exports { get; set; } = [];

    public override string Json()
    {
        dynamic obj = new ExpandoObject();
        obj.Type = Type().ToString();
        obj.Statements = new List<string>();
        obj.Exports = new List<string>();

        foreach (StatementNode stmt in Statements)
            obj.Statements.Add(stmt.Json());

        foreach (StatementNode export in Exports)
            obj.Exports.Add(export.Json());

        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }

    public override NodeType Type()
    {
        return NodeType.Program;
    }
}
