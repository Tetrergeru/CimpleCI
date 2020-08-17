namespace Frontend.AST
{
    public interface IPrototype
    {
        int Id();

        string Name();
        
        IASTNode New(params IASTNode[] args);

        int IdxOf(string name);

        bool Is(IPrototype other);
    }
}