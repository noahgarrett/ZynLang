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

    public override string Json()
    {
        dynamic obj = new ExpandoObject();
        obj.Type = Type().ToString();
        obj.ReturnValue = ReturnValue.Json();

        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}
