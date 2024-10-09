namespace ZynLang.AST.Statements;

public class BreakStatementNode : StatementNode
{ 
    public override NodeType Type()
    {
        return NodeType.BreakStatement;
    }
}
