using ZynLang.AST.Literals;

namespace ZynLang.AST.Statements;

public class LetStatementNode(IdentifierLiteralNode name, ExpressionNode value, string valueType) : StatementNode
{
    public IdentifierLiteralNode Name { get; set; } = name;

    public ExpressionNode Value { get; set; } = value;

    public string ValueType { get; set; } = valueType;

    public override NodeType Type()
    {
        return NodeType.LetStatement;
    }
}
