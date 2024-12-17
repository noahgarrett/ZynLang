using Newtonsoft.Json;
using System.Dynamic;

namespace ZynLang.AST.Statements;

public class ReturnStatementNode(ExpressionNode returnValue) : StatementNode
{
    public ExpressionNode ReturnValue { get; set; } = returnValue;

    public override NodeType Type()
    {
        return NodeType.ReturnStatement;
    }

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "ReturnValue", ReturnValue.Json() }
        };

        return obj;
    }
}
