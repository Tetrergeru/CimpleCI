namespace Middleend.Expressions
{
    public class UnaryExpression : Expression
    {
        public readonly OperationKind Operator;

        public readonly Expression Right;
        
        public UnaryExpression(OperationKind @operator, Expression right)
        {
            Operator = @operator;
            Right = right;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitUnaryExpression(this);
    }
}