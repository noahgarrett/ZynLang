namespace ZynLang.AST.Statements;

public class ContinueStatementNode : StatementNode
{
    public override NodeType Type()
    {
        return NodeType.ContinueStatement;
    }
}
