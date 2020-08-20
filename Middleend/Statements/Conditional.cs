using Middleend.Expressions;

namespace Middleend.Statements
{
    public class Conditional : Statement
    {
        public readonly Expression Condition;

        public readonly Block Then;

        public readonly Block Else;

        public Conditional(Expression condition, Block then, Block @else)
        {
            Condition = condition;
            Then = then;
            Else = @else;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitConditional(this);
    }
}