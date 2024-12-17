using Newtonsoft.Json;

namespace ZynLang.AST.Helpers;

public class FunctionParameterNode(string name, string valueType) : Node
{
    public string Name { get; set; } = name;
    public string ValueType { get; set; } = valueType;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Name", Name },
            { "ValueType", ValueType }
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.FunctionParameter;
    }
}
