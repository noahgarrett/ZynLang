using ZynLang.AST.Literals;

namespace ZynLang.AST.Statements;

/// <summary>
/// Ex. `from test import add`
/// Rather than using file paths, this uses identifiers
/// </summary>
public class ImportFromStatementNode(IdentifierLiteralNode palletName) : StatementNode
{
    public IdentifierLiteralNode PalletName { get; set; } = palletName;
    public List<IdentifierLiteralNode> Imports { get; set; } = [];

    public override Dictionary<string, object> Json()
    {
        Dictionary<string, object> obj = new()
        {
            { "Type", Type().ToString() },
            { "PalletName", PalletName.Json() },
            { "Imports", Imports.ConvertAll(import => import.Json()) },
        };

        return obj;
    }

    public override NodeType Type()
    {
        return NodeType.ImportFromStatement;
    }
}
