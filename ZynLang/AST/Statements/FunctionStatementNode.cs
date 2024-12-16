using Newtonsoft.Json;
using System.Dynamic;
using ZynLang.AST.Helpers;
using ZynLang.AST.Literals;

namespace ZynLang.AST.Statements;

public class FunctionStatementNode(List<FunctionParameterNode> parameters, BlockStatementNode body, IdentifierLiteralNode name, string returnType) : StatementNode
{
    public List<FunctionParameterNode> Parameters { get; set; } = parameters;
    public BlockStatementNode Body { get; set; } = body;
    public IdentifierLiteralNode Name { get; set; } = name;
    public string ReturnType { get; set; } = returnType;

    public override NodeType Type()
    {
        return NodeType.FunctionStatement;
    }

    public override string Json()
    {
        dynamic obj = new ExpandoObject();
        obj.Type = Type().ToString();
        obj.Parameters = new List<string>();
        obj.Name = "testing func";
        obj.ReturnType = ReturnType;

        foreach (StatementNode stmt in Body.Statements)
            obj.Statements.Add(stmt.Json());

        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}
