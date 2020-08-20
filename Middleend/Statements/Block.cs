using System;
using System.Collections.Generic;
using Middleend.Types;

namespace Middleend.Statements
{
    public class Block : Statement
    {
        public readonly List<(string name, BaseType type)> Variables 
            = new List<(string name, BaseType type)>();

        public readonly List<Statement> Statements 
            = new List<Statement>();

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitBlock(this);
    }
}