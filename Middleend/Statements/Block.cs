using System;
using System.Collections.Generic;
using System.Linq;
using Middleend.Types;

namespace Middleend.Statements
{
    public class Block : Statement
    {
        public readonly StructType Variables;

        public readonly List<Statement> Statements;

        public Block(StructType variables, List<Statement> statements)
        {
            Variables = variables;
            Statements = statements;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitBlock(this);
    }
}