using LLVMSharp;
using ZynLang.AST.Literals;

namespace ZynLang.AST.Expressions;

public class CallExpressionNode(IdentifierLiteralNode functionName, List<ExpressionNode> arguments) : ExpressionNode
{
    public IdentifierLiteralNode FunctionName { get; set; } = functionName;
    public List<ExpressionNode> Arguments { get; set; } = arguments;

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "FunctionName", FunctionName.Json() },
            { "Arguments", Arguments.ConvertAll(arg => arg.Json()) }
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.CallExpression;
    }
}
