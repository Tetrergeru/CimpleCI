using System;
using System.IO;
using System.Linq;
using Backend;
using Backend.NasmCompiler;
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
            var grammarFile = "Grammars/Gomple/Gomple.gr";
            var frontend = new FrontendPipeline(File.ReadAllText(grammarFile));
            
            
            var programNameBase = "program_0";
            
            var code = File.ReadAllText($"CodeSamples/Gomple/{programNameBase}.go");
            

            Console.WriteLine("; Grammar parsed");

            var inAst = frontend.Parse(code);
            
            //frontend.Print(inAst);
            
            var inGomple = new AstTranslator<GompleAst>().Translate(frontend.PrototypeDictionary, inAst);
            var inTypedGomple = new GompleTypeTranslator().VisitProgram(inGomple.program);
            var inCimple0 = new GompleTranslator().VisitProgram(inTypedGomple);

            Console.WriteLine("; Translated");

            var printer = new ModulePrinter();
            debug = true;
            Debug(printer.VisitModule(inCimple0));
            debug = false;

            var backend = new NasmCompiler();
            var inNasm = backend.VisitModule(inCimple0).Select(o => o.ToString()).ToList();

            Debug(string.Join("\n", inNasm));
            
            File.WriteAllLines($"{programNameBase}.asm", inNasm);
            var prc = System.Diagnostics.Process.Start("CMD.exe", $"/C nasm -f win64 {programNameBase}.asm");
            if (prc == null)
            {
                Console.WriteLine("Could not start nasm compiler");
                return;
            }
            prc.WaitForExit();

            prc = System.Diagnostics.Process.Start("CMD.exe",
                $"/C golink /console {programNameBase}.obj kernel32.dll MSVCRT.dll Ws2_32.dll");
            if (prc == null)
            {
                Console.WriteLine("Could not start golink linker");
                return;
            }
            prc.WaitForExit();

            prc = System.Diagnostics.Process.Start($"{programNameBase}.exe", $"");
            if (prc == null)
            {
                Console.WriteLine("Could not run program");
                return;
            }
            prc.WaitForExit();
            //*/
        }
    }
}