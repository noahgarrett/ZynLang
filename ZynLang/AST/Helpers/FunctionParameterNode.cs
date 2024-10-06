namespace ZynLang.AST.Helpers;

public class FunctionParameterNode : Node
{
    public string Name { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    
    public override NodeType Type()
    {
        return NodeType.FunctionParameter;
    }
}
