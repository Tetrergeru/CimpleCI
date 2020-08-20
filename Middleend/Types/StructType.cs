using System.Collections.Generic;

namespace Middleend.Types
{
    public class StructType : BaseType
    {
        public readonly List<(string name, BaseType type)> Fields;

        public StructType(List<(string name, BaseType type)> fields)
        {
            Fields = fields;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitStructType(this);
    }
}