using ZynLang.AST.Helpers;

namespace ZynLang.AST.Statements;

public class FunctionStatementNode(List<FunctionParameterNode> parameters, BlockStatementNode body, string name, string returnType) : Node
{
    public List<FunctionParameterNode> Parameters { get; set; } = parameters;
    public BlockStatementNode Body { get; set; } = body;
    public string Name { get; set; } = name;
    public string ReturnType { get; set; } = returnType;

    public override NodeType Type()
    {
        return NodeType.FunctionStatement;
    }
}
