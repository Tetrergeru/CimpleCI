using System;
using System.Collections.Generic;

namespace Frontend.Parser
{
    public interface Callable<T>
    {
        T Call(IReadOnlyList<T> args);
    }
    
    internal class Callback : Callable<object>
    {
        private readonly Func<IReadOnlyList<object>, object> _lambda;

        public Callback(Func<IReadOnlyList<object>, object> lambda)
            => _lambda = lambda;

        public object Call(IReadOnlyList<object> args)
            => _lambda(args);
    }
}