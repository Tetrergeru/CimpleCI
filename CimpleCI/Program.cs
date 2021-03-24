using System;
using System.Diagnostics.Tracing;
using System.IO;
using Backend;
using Frontend;
using Frontend.Lexer;

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
            var code = File.ReadAllText("CodeSamples/program_0.c");

            //RegexLexer.DEBUG = true;
            var frontend = new FrontendPipeline(File.ReadAllText("Grammars/Cimple1.gr"));

            var backend = new ModulePrinter();

            Console.WriteLine(backend.VisitModule(Cimple0Translator.Parse(frontend.Parse(code))));
        }
    }
}