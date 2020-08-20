namespace Middleend.Expressions
{
    public abstract class Expression
    {
        public abstract T AcceptVisitor<T>(IModuleVisitor<T> visitor);
    }
}