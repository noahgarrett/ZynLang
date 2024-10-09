using ZynLang.AST.Literals;

namespace ZynLang.AST.Statements;

public class AssignStatementNode(IdentifierLiteralNode identifier, string op, ExpressionNode rightValue) : StatementNode
{
    public IdentifierLiteralNode Identifier { get; set; } = identifier;
    public string Operator { get; set; } = op;
    public ExpressionNode RightValue { get; set; } = rightValue;

    public override NodeType Type()
    {
        return NodeType.AssignStatement;
    }
}
