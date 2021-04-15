using System;

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

        public override int GetHashCode()
            => NumberKind.GetHashCode() ^ BitSize.GetHashCode();

        public override bool Equals(object obj)
            => ReferenceEquals(obj, this) ||
               obj is NumberType nt && nt.NumberKind == NumberKind && nt.BitSize == BitSize;

        public override string ToString()
            => $"{NumberKind switch {NumberKind.SignedInteger => 'i', NumberKind.UnsignedInteger => 'u', NumberKind.Float => 'f', _ => '_'}}{BitSize}";
    }
}