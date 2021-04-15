using System.Collections.Generic;
using System.Linq;

namespace Backend.NasmCompiler
{
    public static class RegisterDevirtualizer
    {
        public static IEnumerable<Operation> DevirtualizeRegisters(IReadOnlyList<Operation> code)
        {
            var ru = new RegisterUsage(code);
            var replace = new Dictionary<int, Register.RegisterId>();
            foreach (var id in ru.GetPrioritisedVirtualRegisters())
            {
                var n = ru.CanNotReplace(id);
                var to = PrioritisedRegisters.First(r => !n.Contains(RegisterUsage.ToReg(r)));
                replace[id] = to;
                ru.Replace(id, RegisterUsage.ToReg(to));
            }

            Operand Replace(Operand op)
            {
                return op switch
                {
                    StubRegister stubRegister => new Register(replace[stubRegister.Number], stubRegister.Size),
                    Memory memory => new Memory(Replace(memory.Address), memory.Size),
                    Shift shift => new Shift(Replace(shift.Register) as AnyRegister, shift.Shft),
                    _ => op
                };
            }

            foreach (var op in code)
            {
                yield return op switch
                {
                    AsmLine asmLine => asmLine,
                    BinaryOperation binaryOperation => new BinaryOperation(
                        binaryOperation.Op,
                        Replace(binaryOperation.Left),
                        Replace(binaryOperation.Right)
                    ),
                    UnaryOperation unaryOperation => new UnaryOperation(
                        unaryOperation.Op,
                        Replace(unaryOperation.Right)
                    ),
                    _ => op,
                };
            }
        }

        private static readonly List<Register.RegisterId> PrioritisedRegisters = new List<Register.RegisterId>
        {
            Register.RegisterId.Si,
            Register.RegisterId.Di,
            Register.RegisterId.R8,
            Register.RegisterId.R9,
            Register.RegisterId.R10,
            Register.RegisterId.R11,
            Register.RegisterId.C,
            Register.RegisterId.D,
            Register.RegisterId.B,
            Register.RegisterId.R12,
            Register.RegisterId.R13,
            Register.RegisterId.R14,
            Register.RegisterId.R15,
        };

        private class RegisterUsage
        {
            private readonly Dictionary<int, HashSet<int>> _uses = new Dictionary<int, HashSet<int>>();

            public RegisterUsage(IEnumerable<Operation> code)
            {
                foreach (var (op, i) in code.Select((v, i) => (v, i)))
                {
                    switch (op)
                    {
                        case BinaryOperation binaryOperation:
                        {
                            var usedSeq = new[]
                            {
                                UsedRegisters(binaryOperation.Left),
                                UsedRegisters(binaryOperation.Right)
                            };
                            foreach (var used in usedSeq.Where(x => x != -1))
                            {
                                if (!_uses.ContainsKey(used))
                                    _uses[used] = new HashSet<int>();
                                _uses[used].Add(i);
                            }

                            break;
                        }
                        case UnaryOperation unaryOperation:
                        {
                            var used = UsedRegisters(unaryOperation.Right);
                            if (used != -1)
                            {
                                if (!_uses.ContainsKey(used))
                                    _uses[used] = new HashSet<int>();
                                _uses[used].Add(i);
                            }

                            break;
                        }
                    }
                }

                foreach (var (reg, from, to) in _uses.Select(kv => (kv.Key, kv.Value.Min(), kv.Value.Max())))
                    for (var i = from + 1; i < to; i++)
                        _uses[reg].Add(i);
            }

            public static int ToReg(Register.RegisterId id) => -((int) id + 1);

            public HashSet<int> CanNotReplace(int register)
                => _uses[register].SelectMany(UsedRegisters).ToHashSet();

            private HashSet<int> UsedRegisters(int line)
                => _uses.Where(kv => kv.Value.Contains(line)).Select(kv => kv.Key).ToHashSet();

            public IEnumerable<int> GetPrioritisedVirtualRegisters()
                => _uses
                    .Where(kv => kv.Key >= 0)
                    .Select(kv => (reg: kv.Key, lines: kv.Value.Max() - kv.Value.Min()))
                    .OrderBy(rl => rl.lines)
                    .Select(rl => rl.reg);

            public void Replace(int from, int to)
            {
                var prev = _uses[from];
                _uses.Remove(from);
                if (!_uses.ContainsKey(to))
                    _uses[to] = new HashSet<int>();
                foreach (var reg in prev)
                    _uses[to].Add(reg);
            }

            private static int UsedRegisters(Operand o)
            {
                return o switch
                {
                    Memory memory => UsedRegisters(memory.Address),
                    Shift shift => UsedRegisters(shift.Register),
                    StubRegister stub => stub.Number,
                    Register reg => ToReg(reg.Id),
                    _ => -1
                };
            }

            private string RegToString(int reg) => reg < 0 ? $"{(Register.RegisterId) (-reg + 1)}" : $"#{reg}";

            public override string ToString()
            {
                return string.Join("\n", _uses.Select(kv => $@"{RegToString(kv.Key)} => {{ {
                        string.Join(", ", kv.Value)
                    } }}"));
            }
        }
    }
}