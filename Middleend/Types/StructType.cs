using System.Collections.Generic;

namespace Middleend.Types
{
    public class StructType : BaseType
    {
        public readonly List<BaseType> Fields;

        public StructType(List<BaseType> fields)
        {
            Fields = fields;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitStructType(this);
    }
}