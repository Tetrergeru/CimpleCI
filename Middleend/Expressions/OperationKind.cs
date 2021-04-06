using System;

namespace Middleend.Expressions
{
    public enum OperationKind
    {
        // Arithmetic
        Add,
        Subtract,
        Multiply,
        Divide,
        Remainder,
        // Bit
        ShiftLeft,
        ShiftRight,
        BitAnd,
        BitOr,
        BitXor,
        BitNot,
        // Logic
        And,
        Or,
        Not,
        // Comparison
        Less,
        LessEqual,
        Equal,
        NotEqual,
        GreaterEqual,
        Greater,
        // Pointer
        Dereference,
        Reference,
        // Struct
        GetField,
    }

    public static class OperationKindClass
    {
        public static OperationKind ParseOp(string operation, bool binary)
            => operation switch
            {
                "+" => OperationKind.Add,
                "-" => OperationKind.Subtract,
                "*" => binary ? OperationKind.Multiply : OperationKind.Dereference,
                "/" => OperationKind.Divide,
                "%" => OperationKind.Remainder,
                "<<" => OperationKind.ShiftLeft,
                ">>" => OperationKind.ShiftRight,
                "&" => binary ? OperationKind.BitAnd : OperationKind.Reference,
                "|" => OperationKind.BitOr,
                "^" => OperationKind.BitXor,
                "~" => OperationKind.BitNot,
                "&&" => OperationKind.And,
                "||" => OperationKind.Or,
                "!" => OperationKind.Not,
                "<" => OperationKind.Less,
                "<=" => OperationKind.LessEqual,
                "==" => OperationKind.Equal,
                "!=" => OperationKind.NotEqual,
                ">=" => OperationKind.GreaterEqual,
                ">" => OperationKind.Greater,
                "." => OperationKind.GetField,
                _ => throw new ArgumentException()
            };
        
        public static string ToSymbol(this OperationKind operationKind)
            => operationKind switch
            {
                OperationKind.Add => "+",
                OperationKind.Subtract => "-",
                OperationKind.Multiply => "*",
                OperationKind.Divide => "/",
                OperationKind.Remainder => "%",
                OperationKind.ShiftLeft => "<<",
                OperationKind.ShiftRight => ">>",
                OperationKind.BitAnd => "&",
                OperationKind.BitOr => "|",
                OperationKind.BitXor => "^",
                OperationKind.BitNot => "~",
                OperationKind.And => "&&",
                OperationKind.Or => "||",
                OperationKind.Not => "!",
                OperationKind.Less => "<",
                OperationKind.LessEqual => "<=",
                OperationKind.Equal => "==",
                OperationKind.NotEqual => "!=",
                OperationKind.GreaterEqual => ">=",
                OperationKind.Greater => ">",
                OperationKind.Dereference => "*",
                OperationKind.Reference => "&",
                OperationKind.GetField => ".",
                _ => throw new ArgumentException()
            };
    }
}