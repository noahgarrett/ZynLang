namespace ZynLang.AST.Helpers;

public class FunctionParameterNode(string name, string valueType) : Node
{
    public string Name { get; set; } = name;
    public string ValueType { get; set; } = valueType;
    
    public override NodeType Type()
    {
        return NodeType.FunctionParameter;
    }
}
