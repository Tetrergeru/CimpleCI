using System;
using System.IO;
using System.Linq;
using Frontend;
using Frontend.Lexer;

namespace CimpleCI
{
    static class Program
    {
        private const bool debug = false;
        public static void Debug(string text)
        {
            if (debug)
                Console.WriteLine(text);
        }
        
        static void Main(string[] args)
        {
            var code = File.ReadAllText("program_4.c");//"- a+(a + b * c); v + (\n\n b*c) + a; c; /*d+h_e_l_l_o;*/";
            
            var grammar = File.ReadAllText("Cimple0.gr").Split("#Lex")
                .SelectMany(s => s.Split("#AST").SelectMany(s => s.Split("#Grammar"))).ToList();
            var lexer = ParsersParser.ParseLexer(grammar[1]);
            
            var pd = ParsersParser.ParseAST(grammar[2]);
            var parser = ParsersParser.ParseGrammar(grammar[3], lexer, pd);

            Console.WriteLine("===== Ready =====");
            //RegexLexer.DEBUG = true;
            
            var result = parser.Parse(lexer.ParseLexemes(code).ToList());
            if (result == null)
            {
                Console.WriteLine("ERROR");
                return;
            }

            result.Print(lexer.SymbolDictionary());
        }
    }
}