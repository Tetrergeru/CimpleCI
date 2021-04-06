namespace Middleend.Expressions
{
    public class MagicExpression : Expression
    {
        public string Name;

        public MagicExpression(string name)
        {
            Name = name;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitMagicExpression(this);
    }
}