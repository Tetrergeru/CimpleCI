using Middleend.Expressions;

namespace Middleend.Statements
{
    public class Cycle : Statement
    {
        public readonly Expression Condition;

        public readonly Block Body;

        public Cycle(Expression condition, Block body)
        {
            Condition = condition;
            Body = body;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitCycle(this);
    }
}