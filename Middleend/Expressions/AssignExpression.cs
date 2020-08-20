namespace Middleend.Expressions
{
    public class AssignExpression : Expression
    {
        public readonly Expression Left;
        
        public readonly Expression Right;
        
        public readonly OperationKind? PreAssignOperator;

        public AssignExpression(Expression left, Expression right, OperationKind? preAssignOperator = null)
        {
            Left = left;
            Right = right;
            PreAssignOperator = preAssignOperator;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitAssignExpression(this);
    }
}