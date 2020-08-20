namespace Middleend.Types
{
    public abstract class BaseType
    {
        public abstract T AcceptVisitor<T>(IModuleVisitor<T> visitor);
    }
}