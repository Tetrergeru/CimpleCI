namespace Middleend.Types
{
    public enum NumberKind
    {
        SignedInteger,
        UnsignedInteger,
        Float
    }

    public class NumberType : BaseType
    {
        public readonly NumberKind NumberKind;

        public readonly int BitSize;

        public NumberType(NumberKind numberKind, int bitSize)
        {
            NumberKind = numberKind;
            BitSize = bitSize;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitNumberType(this);

    }
}