namespace ZynLang.AST.Statements;

public class ForStatementNode : Node
{
    public override NodeType Type()
    {
        return NodeType.ForStatement;
    }
}
