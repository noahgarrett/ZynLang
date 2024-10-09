namespace ZynLang.AST.Statements;

public class ForStatementNode(LetStatementNode varDeclaration, ExpressionNode condition, AssignStatementNode action, BlockStatementNode body) : StatementNode
{
    public LetStatementNode VarDeclaration { get; set; } = varDeclaration;
    public ExpressionNode Condition { get; set; } = condition;
    public AssignStatementNode Action { get; set; } = action;
    public BlockStatementNode Body { get; set; } = body;

    public override NodeType Type()
    {
        return NodeType.ForStatement;
    }
}
