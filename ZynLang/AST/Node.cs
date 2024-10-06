namespace ZynLang.AST;

public enum NodeType
{
    Program,

    // Statements
    ExpressionStatement, LetStatement, FunctionStatement, BlockStatement, ReturnStatement,
    AssignStatement, IfStatement, WhileStatement, BreakStatement, ContinueStatement, ForStatement,
    ImportStatement,

    // Expressions
    InfixExpression, CallExpression, PrefixExpression, PostfixExpression, DotExpression,

    // Literals
    IntegerLiteral, FloatLiteral, IdentifierLiteral, BooleanLiteral, StringLiteral,

    // Helpers
    FunctionParameter
}

public abstract class Node
{
    public abstract NodeType Type();
}

public abstract class StatementNode : Node { }
public abstract class ExpressionNode : Node { }
