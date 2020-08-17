using System;
using Frontend.Lexer;

namespace Frontend.AST
{
    public class ASTLeaf : IASTNode
    {
        private readonly Token _token;

        private readonly int _id;

        public int Id() => _id;
        
        public ASTLeaf(Token token, int id)
        {
            _token = token;
            _id = id;
        }

        public IASTNode this[string name] 
            => throw new Exception("Cannot get field of a leaf.");

        public void Print(SymbolDictionary sd, string offset = "")
        {
            Console.WriteLine($"{offset}Token({sd[_token.Id].name}): '{_token.Text}' on line {_token.Line}");
        }
    }
}