namespace ZynLang.AST.Statements;

public class LetStatementNode : Node
{
    public string Name { get; set; } = string.Empty;

    public ExpressionNode Value { get; set; }

    public string ValueType { get; set; } = string.Empty;

    public override NodeType Type()
    {
        return NodeType.LetStatement;
    }
}
