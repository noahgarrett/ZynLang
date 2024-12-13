using ZynLang.AST;
using ZynLang.Execution;

namespace ZynLangTests.Execution.Tests;

[TestClass]
public class ParserTests
{
    [TestMethod]
    [DataRow("let a: int = -4;", NodeType.LetStatement)]
    [DataRow("a = 5;", NodeType.AssignStatement)]
    [DataRow("break;", NodeType.BreakStatement)]
    [DataRow("continue;", NodeType.ContinueStatement)]
    [DataRow("5 + 5;", NodeType.ExpressionStatement)]
    [DataRow("for (let i: int = 0; i < 10; i++) { }", NodeType.ForStatement)]
    [DataRow("if x == 2 { } else { }", NodeType.IfStatement)]
    [DataRow("return 1;", NodeType.ReturnStatement)]
    [DataRow("while (x == 2) { }", NodeType.WhileStatement)]
    [DataRow("fn main() int { }", NodeType.FunctionStatement)]
    public void NodeTypeTests(string test, NodeType expectedNT)
    {
        Lexer lexer = new(test);
        Parser parser = new(lexer);

        ProgramNode programNode = parser.ParseProgram();
        if (parser.Errors.Count > 0)
        {
            string errorOutput = string.Empty;

            foreach (var error in parser.Errors)
                errorOutput += $"\n- Parser Error: {error}";

            Assert.Fail(errorOutput);
        }

        if (programNode.Statements.Count == 0)
            Assert.Fail("The program statements length was zero.");

        Assert.AreEqual(expectedNT, programNode.Statements[0].Type());
    }
}
