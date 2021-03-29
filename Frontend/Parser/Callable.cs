using System.Collections.Generic;

namespace Frontend.Parser
{
    public interface Callable<T>
    {
        T Call(IReadOnlyList<T> args);
    }
}