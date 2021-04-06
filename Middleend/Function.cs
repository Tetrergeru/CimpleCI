using System.Collections.Generic;
using Middleend.Statements;
using Middleend.Types;

namespace Middleend
{
    public class Function : IEntity
    {
        public readonly FunctionType Type;

        public readonly Block Code;

        public Function(FunctionType type, Block code)
        {
            Type = type;
            Code = code;
        }

        public T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitFunction(this);
    }
}