using System.Collections.Generic;
using Middleend.Statements;
using Middleend.Types;

namespace Middleend
{
    public class Function : IEntity
    {
        public readonly string Name;

        public readonly FunctionType Type;

        public readonly List<Statement> Code;

        public Function(string name, FunctionType type, List<Statement> code)
        {
            Name = name;
            Type = type;
            Code = code;
        }

        public T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitFunction(this);
    }
}