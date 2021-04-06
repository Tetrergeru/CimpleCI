namespace Middleend.Expressions
{
    public class NameExpression : Expression
    {
        public readonly int Depth;

        public NameExpression(int depth)
        {
            Depth = depth;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitNameExpression(this);
    }
}