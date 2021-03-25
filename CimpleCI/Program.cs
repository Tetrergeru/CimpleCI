using System;
using System.IO;
using Backend;
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
            
            var code = File.ReadAllText("CodeSamples/tcp_server.c");//"2 + (2 + 2)";//

            //RegexLexer.DEBUG = true;
            var frontend = new FrontendPipeline(File.ReadAllText("Grammars/Cimple1.gr"));//new FrontendPipeline(File.ReadAllText("Grammars/grammar_0.gr")); //
            Console.WriteLine("Grammar parsed");
            
            //frontend.Print(frontend.Parse(code));
            var backend = new ModulePrinter();
            Console.WriteLine(backend.VisitModule(Cimple0Translator.Parse(frontend.Parse(code))));
        }
    }
}