namespace ZynLang.AST.Literals;

public class HashLiteralNode(Dictionary<ExpressionNode, ExpressionNode> pairs) : ExpressionNode
{
    public Dictionary<ExpressionNode, ExpressionNode> Pairs { get; set; } = pairs;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Pairs", Pairs.ToDictionary(pair => pair.Key.Json(), pair => pair.Value.Json()) }
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.HashLiteral;
    }
}
