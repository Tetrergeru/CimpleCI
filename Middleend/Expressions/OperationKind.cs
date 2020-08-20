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
    }

    public static class OperationKindClass
    {
        public static string ToString(this OperationKind operationKind)
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
                _ => throw new ArgumentException()
            };
    }
}