namespace Middleend
{
    public interface IEntity
    {
        T AcceptVisitor<T>(IModuleVisitor<T> visitor);
    }
}