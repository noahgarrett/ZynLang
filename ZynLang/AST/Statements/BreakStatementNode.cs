namespace ZynLang.AST.Statements;

public class BreakStatementNode : Node
{ 
    public override NodeType Type()
    {
        return NodeType.BreakStatement;
    }
}
