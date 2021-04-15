namespace Middleend.Expressions
{
    public class GetFieldExpression : Expression
    {
        public Expression Left;

        public int Field;

        public GetFieldExpression(Expression left, int field)
        {
            Left = left;
            Field = field;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitGetFieldExpression(this);
    }
}