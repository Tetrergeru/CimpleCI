using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.Lexer;

// ReSharper disable all

namespace CimpleCI.Translators.Gomple
{
    public class GompleAst
    {
        public Program program;

        public class Program
        {
            public List<Function> Functions;
            public List<TypeDef> Types;
        }

        public class Function
        {
            public Token Name;
            public CallableType Type;
            public Block Body;
        }

        public class Variable
        {
            public Token Name;
            public Type Type;

            public override bool Equals(object obj)
                => obj is Variable v && v.Name == Name && v.Type == Type;

            public override int GetHashCode()
                => HashCode.Combine(Name, Type);

            public override string ToString()
                => $"{Name.Text}: {Type}";
        }

        public class TypeDef
        {
            public Token Name;
            public Type Type;
        }

        public class Type
        {
        }

        public class CallableType : Type
        {
            public StructType Args;
            public Type Result;
        }
        
        public class FunctionType : CallableType
        {
            public override bool Equals(object obj)
                => obj is FunctionType fn && Args == fn.Args && Result == fn.Result;

            public override int GetHashCode()
                => "Function".GetHashCode() ^ Args.GetHashCode() ^ Result.GetHashCode();
            
            public override string ToString()
                => $"fn ({string.Join(", ", Args)}) {Result}";
        }
        
        public class MethodType : CallableType
        {
            public Variable Sender;
            
            public override bool Equals(object obj)
                => obj is MethodType mth && Args == mth.Args && Result == mth.Result && Sender == mth.Sender;

            public override int GetHashCode()
                => "Function".GetHashCode() ^ Args.GetHashCode() ^ Result.GetHashCode() ^ Sender.GetHashCode();
            
            public override string ToString()
                => $"fn ({string.Join(", ", Args)}) {Result}";
        }
        
        public class StructType : Type
        {
            public List<Variable> Variables;

            public override bool Equals(object obj)
                => obj is StructType st && Variables.Zip(st.Variables).All(v => v.First == v.Second);

            public override int GetHashCode()
                => (Variables != null ? Variables.Aggregate(0, (i, v) => v.GetHashCode() ^ i).GetHashCode() : 0);

            public override string ToString()
                => $"({string.Join(", ", Variables)})";
        }

        public class PointerType : Type
        {
            public Type To;
            
            public override bool Equals(object obj)
                => obj is PointerType pt && To == pt.To;

            public override int GetHashCode()
                => "Pointer".GetHashCode() ^ To.GetHashCode();

            public override string ToString()
                => $"*{To}";
        }
        
        public class IntegerType : Type
        {
            public override bool Equals(object obj)
                => obj is IntegerType;

            public override int GetHashCode()
                => "Integer".GetHashCode();
            
            public override string ToString()
                => "int";
        }

        public class FloatType : Type
        {
            public override bool Equals(object obj)
                => obj is FloatType;

            public override int GetHashCode()
                => "Float".GetHashCode();
            
            public override string ToString()
                => "float";
        }

        public class VoidType : Type
        {
            public override bool Equals(object obj)
                => obj is VoidType;

            public override int GetHashCode()
                => "Void".GetHashCode();
            
            public override string ToString()
                => "void";
        }

        public class TypeRef : Type
        {
            public Token Name;

            public override bool Equals(object obj)
                => obj is TypeRef tr && tr.Name.Text == Name.Text;

            public override int GetHashCode()
                => "Ref".GetHashCode() ^ Name.Text.GetHashCode();
            
            public override string ToString()
                => Name.Text;
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
            public List<GetterField> Field;
        }

        public class GetterField
        {
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