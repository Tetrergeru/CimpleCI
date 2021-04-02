using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime;
using Backend;
using CimpleCI.Translators.Gomple;
using Frontend;

namespace CimpleCI
{
    static class Program
    {
        private static bool debug = false;

        public static void Debug(string text)
        {
            if (debug)
                Console.WriteLine(text);
        }

        static void Main()
        {
            //var pl = new ParsersLexer();

            var code = "func main(x int) { return x.y; }"; //File.ReadAllText("CodeSamples/tcp_server.c");//

            var frontend =
                new FrontendPipeline(
                    File.ReadAllText(
                        "Grammars/Gomple/Gomple.gr")); //new FrontendPipeline(File.ReadAllText("Grammars/grammar_0.gr")); //

            Console.WriteLine("Grammar parsed");

            //frontend.Print(frontend.Parse(code));
            
            var ast = new AstTranslator<GompleAst>().Translate(frontend.PrototypeDictionary, frontend.Parse(code));
            
            //var backend = new ModulePrinter();
            //Console.WriteLine(backend.VisitModule(Cimple0Translator.Parse(frontend.Parse(code))));
            //Console.WriteLine("File parsed");
        }
    }
}