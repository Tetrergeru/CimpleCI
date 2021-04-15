namespace Middleend.Types
{
    public class ArrayType : BaseType
    {
        public readonly BaseType ElemType;

        public readonly int Length;

        public ArrayType(BaseType elemType, int length)
        {
            ElemType = elemType;
            Length = length;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitArrayType(this);

        public override int GetHashCode()
            => ElemType.GetHashCode() ^ Length.GetHashCode();

        public override bool Equals(object obj)
            => ReferenceEquals(obj, this) ||
               obj is ArrayType at && at.ElemType.Equals(ElemType) && at.Length == Length;


        public override string ToString() => $"[{ElemType};{Length}]";
    }
}