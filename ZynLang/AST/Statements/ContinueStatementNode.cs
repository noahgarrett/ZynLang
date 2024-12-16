namespace ZynLang.AST.Statements;

public class ContinueStatementNode : StatementNode
{
    public override string Json()
    {
        throw new NotImplementedException();
    }

    public override NodeType Type()
    {
        return NodeType.ContinueStatement;
    }
}
