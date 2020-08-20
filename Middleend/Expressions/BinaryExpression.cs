namespace Middleend.Expressions
{
    public class BinaryExpression : Expression
    {
        public readonly Expression Left;
        
        public readonly OperationKind Operator;

        public readonly Expression Right;
        
        public BinaryExpression(Expression left, OperationKind @operator, Expression right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitBinaryExpression(this);
    }
}