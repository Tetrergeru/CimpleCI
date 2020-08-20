using Middleend.Types;

namespace Middleend.Expressions
{
    public class ConstExpression : Expression
    {
        public readonly BaseType Type;

        public readonly object Value;

        public ConstExpression(BaseType type, object value)
        {
            Type = type;
            Value = value;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitConstExpression(this);
    }
}