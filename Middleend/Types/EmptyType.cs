namespace Middleend.Types
{
    public class EmptyType : BaseType
    {
        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitEmptyType(this);
    }
}