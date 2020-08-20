namespace Middleend.Expressions
{
    public class ParExpression : Expression
    {
        public readonly Expression Expr;

        public ParExpression(Expression expr)
        {
            Expr = expr;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitParExpression(this);
    }
}