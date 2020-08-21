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
    }
}