using ZynLang.AST.Literals;

namespace ZynLang.AST.Expressions;

public class CallExpressionNode(IdentifierLiteralNode functionName, List<ExpressionNode> arguments) : ExpressionNode
{
    public IdentifierLiteralNode FunctionName { get; set; } = functionName;
    public List<ExpressionNode> Arguments { get; set; } = arguments;

    public override NodeType Type()
    {
        return NodeType.CallExpression;
    }
}
