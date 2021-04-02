using System.Collections.Generic;
using Frontend.Lexer;

namespace CimpleCI.Translators.Gomple
{
    public class GompleAst
    {
        public Program program;
        
        public class Program
        {
            public List<Callable> Functions;
            public List<TypeDef> Types;
        }

        public class Callable
        {
        }

        public class Function : Callable
        {
            public Token Name;
            public List<Variable> Params;
            public Type Result;
            public Block Body;
        }

        public class Method : Callable
        {
            public Token Name;
            public Variable Sender;
            public List<Variable> Params;
            public Type Result;
            public Block Body;
        }

        public class Variable
        {
            public Token Name;
            public Type Type;
        }

        public class TypeDef
        {
            public Token Name;
            public Type Type;
        }

        public class Type
        {
        }

        public class StructType : Type
        {
            public List<Variable> Variables;
        }

        public class IntegerType : Type
        {
        }

        public class FloatType : Type
        {
        }

        public class VoidType : Type
        {
        }

        public class TypeRef : Type
        {
            public Token Name;
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
            public Type Type;
        }

        public class VarAssignStatement : Statement
        {
            public Token Name;
            public Type Type;
            public Expression Value;
        }

        public class Expression
        {
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