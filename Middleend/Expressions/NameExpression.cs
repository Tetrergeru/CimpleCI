namespace Middleend.Expressions
{
    public class NameExpression : Expression
    {
        public readonly string Name;

        public NameExpression(string name)
        {
            Name = name;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitNameExpression(this);
    }
}