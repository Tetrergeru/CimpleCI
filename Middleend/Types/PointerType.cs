using System.Threading;

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

        public override int GetHashCode()
            => "pointer".GetHashCode() ^ To.GetHashCode();

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || obj is PointerType pt && pt.To.Equals(To);

        public override string ToString()
            => $"*{To}";
    }
}