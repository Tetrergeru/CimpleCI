using System;
using System.Collections.Generic;
using System.Linq;
using Middleend;
using Middleend.Types;

namespace Backend.NasmCompiler
{
    public class FunctionData
    {
        private NasmCompiler _compiler;

        public Dictionary<int, (bool onStack, int place)> Params;

        public StructType StackStruct;

        public int Registers;

        public FunctionData(FunctionType function, NasmCompiler compiler)
        {
            _compiler = compiler;
            MakeParams(function);
        }

        private void MakeParams(FunctionType function)
        {
            Registers = 0;
            var stacks = 0;
            var fields = function
                .Params
                .Fields;
            var paramData = fields
                .Select(v => (
                    onStack: !((v is NumberType nt && nt.BitSize <= 64 || v is PointerType) && Registers++ < 4), v))
                .Select((v, i) => (typeIdx: i, v.onStack, idx: v.onStack ? stacks++ : Registers - 1))
                .ToList();
            var str = new StructType(paramData.Where(v => v.onStack).Select(v => fields[v.typeIdx]).ToList());
            Params = paramData
                .ToDictionary(
                    v => v.typeIdx,
                    v => (v.onStack, v.onStack ? TypeEvaluator.GetOffset(str, v.idx) : v.idx)
                );
            StackStruct = str;
        }
    }
}