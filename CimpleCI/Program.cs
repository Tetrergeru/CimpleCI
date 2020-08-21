using System;
using System.IO;
using Backend;
using Frontend;

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
            var code = File.ReadAllText("CodeSamples/tcp_server.c");

            var frontend = new FrontendPipeline(File.ReadAllText("Grammars/Cimple0.gr"));
            var backend = new ModulePrinter();

            Console.WriteLine(backend.VisitModule(Cimple0Translator.Parse(frontend.Parse(code))));
        }
    }
}