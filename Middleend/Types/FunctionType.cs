using System.Collections.Generic;

namespace Middleend.Types
{
    public class FunctionType : BaseType
    {
        public readonly List<(string name, BaseType type)> Params;

        public readonly BaseType Result;

        public FunctionType(List<(string name, BaseType type)> @params, BaseType result)
        {
            Params = @params;
            Result = result;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitFunctionType(this);
    }
}