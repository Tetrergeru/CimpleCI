using System.Collections.Generic;
using System.Linq;

namespace Middleend.Types
{
    public class StructType : BaseType
    {
        public readonly List<BaseType> Fields;

        public StructType(List<BaseType> fields)
        {
            Fields = fields;
        }

        public StructType(params BaseType[] fields)
        {
            Fields = fields.ToList();
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitStructType(this);


        private bool _visiting;

        public override int GetHashCode()
        {
            if (_visiting)
                return 0;
            _visiting = true;
            var result = Fields.Aggregate(0, (a, b) => a ^ b.GetHashCode());
            _visiting = false;

            return result;
        }

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) ||
               obj is StructType st && st.Fields.Count == Fields.Count && st.Fields.Zip(Fields, (a, b) => a.Equals(b)).All(x => x);

        public override string ToString()
        {
            if (_visiting)
                return "##";
            _visiting = true;
            var result = $"{{{string.Join(",", Fields)}}}";
            _visiting = false;
            return result;
        }
    }
}