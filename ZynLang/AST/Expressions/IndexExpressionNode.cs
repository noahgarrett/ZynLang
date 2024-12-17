using LLVMSharp;

namespace ZynLang.AST.Expressions;

public class IndexExpressionNode(ExpressionNode leftNode, ExpressionNode indexNode) : ExpressionNode
{
    public ExpressionNode LeftNode { get; set; } = leftNode;
    public ExpressionNode IndexNode { get; set; } = indexNode;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "LeftNode", LeftNode.Json() },
            { "IndexNode", IndexNode.Json() },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.IndexExpression;
    }
}
