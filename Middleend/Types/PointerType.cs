namespace Middleend.Types
{
    public class PointerType : BaseType
    {
        public readonly BaseType To;

        public PointerType(BaseType to)
        {
            To = to;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitPointerType(this);
    }
}