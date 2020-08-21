using System.Collections.Generic;

namespace Middleend
{
    public class Module
    {
        public readonly List<IEntity> Entities;

        public Module(List<IEntity> entities)
        {
            Entities = entities;
        }

        public T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitModule(this);
    }
}