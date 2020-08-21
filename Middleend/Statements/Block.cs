using System;
using System.Collections.Generic;
using Middleend.Types;

namespace Middleend.Statements
{
    public class Block : Statement
    {
        public readonly List<(string name, BaseType type)> Variables;

        public readonly List<Statement> Statements;

        public Block(List<(string name, BaseType type)> variables, List<Statement> statements)
        {
            Variables = variables;
            Statements = statements;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitBlock(this);
    }
}