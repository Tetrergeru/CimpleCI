using Middleend.Expressions;

namespace Middleend.Statements
{
    public class ExpressionStatement : Statement
    {
        public readonly Expression Expr;

        public ExpressionStatement(Expression expr)
        {
            Expr = expr;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitExpressionStatement(this);
    }
}