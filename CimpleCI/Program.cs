using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Windows.Markup;
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
            var code = File.ReadAllText("CodeSamples/program_2.c1");//"2 + (2 + 2)";//

            //RegexLexer.DEBUG = true;
            var frontend = new FrontendPipeline(File.ReadAllText("Grammars/Cimple1.gr"));//new FrontendPipeline(File.ReadAllText("Grammars/grammar_0.gr")); //
            
            frontend.Print(frontend.Parse(code));
            var backend = new ModulePrinter();
            //Console.WriteLine(backend.VisitModule(Cimple0Translator.Parse(frontend.Parse(code))));
        }
    }
}