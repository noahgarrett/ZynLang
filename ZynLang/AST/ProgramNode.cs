namespace ZynLang.AST;

public class ProgramNode : Node
{
    public List<StatementNode> Statements { get; set; } = [];

    public override NodeType Type()
    {
        return NodeType.Program;
    }
}
