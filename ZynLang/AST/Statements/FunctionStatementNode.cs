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

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Name", Name.Json() },
            { "ReturnType", ReturnType },
            { "Parameters", Parameters.ConvertAll(param => param.Json()) },
            { "Body", Body.Json() }
        };

        return obj;
    }
}
