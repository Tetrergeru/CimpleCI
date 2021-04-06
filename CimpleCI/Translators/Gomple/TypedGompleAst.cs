using System.Collections.Generic;
using Frontend.Lexer;

// ReSharper disable all

namespace CimpleCI.Translators.Gomple
{
    public class TypedGompleAst
    {
        public Program program;

        public class Program
        {
            public List<Function> Functions;
            public Dictionary<string, GompleAst.Type> NamedTypes;
            public Dictionary<(string name, GompleAst.Type receiver), GompleAst.Type> Methods;
        }

        public class Function
        {
            public Token Name;
            public GompleAst.CallableType Type;
            public Block Body;
        }

        public class Block
        {
            public List<Statement> Statements;
        }

        public class Statement
        {
        }

        public class ReturnStatement : Statement
        {
            public Expression Value;
        }

        public class ReturnNothingStatement : Statement
        {
        }

        public class ExpressionStatement : Statement
        {
            public Expression Expression;
        }

        public class AssignStatement : Statement
        {
            public Expression Left;
            public Expression Right;
        }

        public class VarStatement : Statement
        {
            public Token Name;
            public GompleAst.Type Type;
        }

        public class VarAssignStatement : Statement
        {
            public Token Name;
            public GompleAst.Type Type;
            public Expression Value;
        }

        public class Expression
        {
            public GompleAst.Type Type;
        }

        public class BinExpression : Expression
        {
            public Expression Left;
            public Token Op;
            public Expression Right;
        }

        public class CallExpression : Expression
        {
            public Expression Function;
            public List<Expression> Params;
        }

        public class GetExpression : Expression
        {
            public Expression Struct;
            public Token Field;
        }

        public class NameExpression : Expression
        {
            public Token Name;
        }

        public class IntegerConstExpression : Expression
        {
            public Token Value;
        }

        public class FloatConstExpression : Expression
        {
            public Token Value;
        }
    }
}