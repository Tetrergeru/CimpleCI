namespace Middleend.Statements
{
    public abstract class Statement
    {
        public abstract T AcceptVisitor<T>(IModuleVisitor<T> visitor);
    }
}