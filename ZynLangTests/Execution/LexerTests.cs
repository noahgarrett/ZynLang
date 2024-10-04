using ZynLang.Models;

namespace ZynLang.Execution.Tests
{
    [TestClass]
    public class LexerTests
    {
        [TestMethod]
        [DataRow("\0", TokenType.EOF)]
        [DataRow("myVarGayAF", TokenType.IDENT)]
        [DataRow("15", TokenType.INT)]
        [DataRow("42.25", TokenType.FLOAT)]
        [DataRow("\"EAT bubbs\"", TokenType.STRING)]
        [DataRow("+", TokenType.PLUS)]
        [DataRow("-", TokenType.MINUS)]
        [DataRow("*", TokenType.ASTERISK)]
        [DataRow("/", TokenType.SLASH)]
        [DataRow("!", TokenType.BANG)]
        [DataRow("++", TokenType.PLUS_PLUS)]
        [DataRow("--", TokenType.MINUS_MINUS)]
        [DataRow("-=", TokenType.MINUS_EQ)]
        [DataRow("=", TokenType.EQ)]
        [DataRow("+=", TokenType.PLUS_EQ)]
        [DataRow("-=", TokenType.MINUS_EQ)]
        [DataRow("*=", TokenType.MUL_EQ)]
        [DataRow("/=", TokenType.DIV_EQ)]
        [DataRow("<", TokenType.LT)]
        [DataRow("<=", TokenType.LT_EQ)]
        [DataRow(">", TokenType.GT)]
        [DataRow(">=", TokenType.GT_EQ)]
        [DataRow("==", TokenType.EQ_EQ)]
        [DataRow("!=", TokenType.NOT_EQ)]
        [DataRow(":", TokenType.COLON)]
        [DataRow(",", TokenType.COMMA)]
        [DataRow(";", TokenType.SEMICOLON)]
        [DataRow("(", TokenType.LPAREN)]
        [DataRow(")", TokenType.RPAREN)]
        [DataRow("{", TokenType.LBRACE)]
        [DataRow("}", TokenType.RBRACE)]
        public void TokenTypes_Test(string test, TokenType expectedTT)
        {
            Lexer lexer = new(test);
            Token token = lexer.NextToken();

            Assert.AreEqual(token.Type, expectedTT);
        }

        [TestMethod]
        [DataRow("let", TokenType.LET)]
        [DataRow("fn", TokenType.FN)]
        [DataRow("return", TokenType.RETURN)]
        [DataRow("if", TokenType.IF)]
        [DataRow("else", TokenType.ELSE)]
        [DataRow("true", TokenType.TRUE)]
        [DataRow("false", TokenType.FALSE)]
        [DataRow("while", TokenType.WHILE)]
        [DataRow("break", TokenType.BREAK)]
        [DataRow("continue", TokenType.CONTINUE)]
        [DataRow("for", TokenType.FOR)]
        [DataRow("import", TokenType.IMPORT)]
        public void Keywords_Test(string test, TokenType expectedTT)
        {
            Lexer lexer = new(test);
            Token token = lexer.NextToken();

            Assert.AreEqual(token.Type, expectedTT);
        }

        [TestMethod]
        [DataRow("lit", TokenType.LET)]
        [DataRow("bruh", TokenType.FN)]
        [DataRow("pause", TokenType.RETURN)]
        [DataRow("sus", TokenType.IF)]
        [DataRow("imposter", TokenType.ELSE)]
        [DataRow("nocap", TokenType.TRUE)]
        [DataRow("cap", TokenType.FALSE)]
        [DataRow("weee", TokenType.WHILE)]
        [DataRow("yeet", TokenType.BREAK)]
        [DataRow("anothaone", TokenType.CONTINUE)]
        [DataRow("dab", TokenType.FOR)]
        [DataRow("gib", TokenType.IMPORT)]
        public void AltKeywords_Test(string test, TokenType expectedTT)
        {
            Lexer lexer = new(test);
            Token token = lexer.NextToken();

            Assert.AreEqual(token.Type, expectedTT);
        }

        [TestMethod]
        [DataRow("int", TokenType.TYPE)]
        [DataRow("float", TokenType.TYPE)]
        [DataRow("bool", TokenType.TYPE)]
        [DataRow("str", TokenType.TYPE)]
        [DataRow("void", TokenType.TYPE)]
        public void TypeKeywords_Test(string test, TokenType expectedTT)
        {
            Lexer lexer = new(test);
            Token token = lexer.NextToken();

            Assert.AreEqual(token.Type, expectedTT);
        }
    }
}