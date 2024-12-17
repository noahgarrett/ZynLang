namespace ZynLang.AST.Literals;

public class ArrayLiteralNode(List<ExpressionNode> elements) : ExpressionNode
{
    public List<ExpressionNode> Elements { get; set; } = elements;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "Elements", Elements.ConvertAll(e => e.Json()) }
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.ArrayLiteral;
    }
}
