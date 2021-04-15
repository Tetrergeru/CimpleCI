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

        public override int GetHashCode()
            => Params.GetHashCode() ^ Result.GetHashCode();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
                return true;
            if (!(obj is FunctionType ft))
                return false;
            var result = ft.Params.Equals(Params);
            result &= ft.Result.Equals(Result);
            return result;
        }

        public override string ToString()
            => $"fn {Params} -> {Result}";
    }
}