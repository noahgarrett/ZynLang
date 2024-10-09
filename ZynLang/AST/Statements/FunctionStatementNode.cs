using ZynLang.AST.Helpers;
using ZynLang.AST.Literals;

namespace ZynLang.AST.Statements;

public class FunctionStatementNode(List<FunctionParameterNode> parameters, BlockStatementNode body, IdentifierLiteralNode name, string returnType) : StatementNode
{
    public List<FunctionParameterNode> Parameters { get; set; } = parameters;
    public BlockStatementNode Body { get; set; } = body;
    public IdentifierLiteralNode Name { get; set; } = name;
    public string ReturnType { get; set; } = returnType;

    public override NodeType Type()
    {
        return NodeType.FunctionStatement;
    }
}
