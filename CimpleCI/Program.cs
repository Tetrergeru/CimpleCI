﻿using System;
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

            var code = @"
type Foo struct {
    a int;
    b float;
    c struct { d: int; };
}

func (f Foo) test() {
    f.a = 1;
    f.test();
}

func main(x int) {
    var foo Foo;
    foo.a = 42;
    main(1);
    return;
}
"; //File.ReadAllText("CodeSamples/tcp_server.c");//
            

            var grammarFile = "Grammars/Gomple/Gomple.gr"; //"Grammars/grammar_0.gr"
            var frontend = new FrontendPipeline(File.ReadAllText(grammarFile));

            Console.WriteLine("; Grammar parsed");

            //frontend.Print(frontend.Parse(code));

            var inGomple = new AstTranslator<GompleAst>().Translate(frontend.PrototypeDictionary, frontend.Parse(code));
            var inTypedGomple = new GompleTypeTranslator().VisitProgram(inGomple.program);
            var inCimple0 = new GompleTranslator().VisitProgram(inTypedGomple);
            //Cimple0Translator.Parse(frontend.Parse(code))

            var backend = new ModulePrinter();
            Console.WriteLine(backend.VisitModule(inCimple0));
        }
    }
}