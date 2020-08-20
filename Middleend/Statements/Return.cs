using Middleend.Expressions;

namespace Middleend.Statements
{
    public class Return : Statement
    {
        public readonly Expression Value;

        public Return(Expression value)
        {
            Value = value;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitReturn(this);
    }
}