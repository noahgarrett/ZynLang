namespace ZynLang.AST.Statements;

public class ContinueStatementNode : Node
{
    public override NodeType Type()
    {
        return NodeType.ContinueStatement;
    }
}
