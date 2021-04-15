namespace Middleend.Types
{
    public class EmptyType : BaseType
    {
        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitEmptyType(this);

        public override int GetHashCode()
            => "Empty".GetHashCode();

        public override bool Equals(object obj)
            => obj is EmptyType;

        public override string ToString() => "unit";
    }
}