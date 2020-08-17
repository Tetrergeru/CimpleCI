namespace Frontend.Lexer
{
    public class Token
    {
        public readonly int Id;

        public readonly string Text;

        public readonly int Line;
        
        public Token(int id, string text, int line)
        {
            Id = id;
            Text = text;
            Line = line;
        }
    }
}