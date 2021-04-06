using System.Collections.Generic;

namespace Middleend.Types
{
    public class FunctionType : BaseType
    {
        public readonly StructType Params;

        public readonly BaseType Result;

        public FunctionType(StructType @params, BaseType result)
        {
            Params = @params;
            Result = result;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitFunctionType(this);
    }
}