using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Markup;
using Middleend;
using Middleend.Expressions;
using Middleend.Statements;
using Middleend.Types;

namespace Backend.NasmCompiler
{
    public class NasmCompiler : IModuleVisitor<IEnumerable<Operation>>
    {
        private Module _module;

        private int _usedRegisters = 0;

        private AnyRegister NewRegister(int size) => LastRegister = new StubRegister(_usedRegisters++, size);

        private AnyRegister LastRegister { get; set; }

        private int _currentFunction;

        private Dictionary<int, (Dictionary<int, (bool onStack, int place)>, StructType stackStruct, int regs)>
            _functionParams;

        private (Dictionary<int, (bool onStack, int place)>, StructType stackStruct, int regs) CurrentFunctionParams =>
            _functionParams[_currentFunction];

        private int _functionFrame = 0;

        private Label _ret;

        public IEnumerable<Operation> VisitModule(Module module)
        {
            yield return new AsmLine("extern printf");
            yield return new AsmLine("extern ExitProcess");
            yield return new AsmLine("section .text");
            yield return new AsmLine("global Start");
            yield return new AsmLine("Start: call Function0");
            yield return new AsmLine("xor rcx, rcx");
            yield return new AsmLine("call ExitProcess");

            foreach (var op in HardCode())
                yield return op;

            _functionParams = module
                .Entities
                .Select((v, i) => (v: v as Function, i))
                .ToDictionary(vi => vi.i, vi => MakeParams(vi.v));

            _module = module;
            foreach (var e in module.Entities)
            {
                foreach (var op in e.AcceptVisitor(this))
                    yield return op;
                _currentFunction++;
            }

            yield return new AsmLine("section .data");
            yield return new AsmLine("fmt:    db \"%d\", 10, 0");
        }

        private IEnumerable<Operation> HardCode()
        {
            yield return new Label("Print");
            yield return Operation.Mov(Register.Rdx, Register.Rcx);
            yield return Operation.Mov(Register.Rcx, new NameOperand("fmt"));
            yield return Operation.Sub(Register.Rsp, 40);
            yield return new UnaryOperation(UnaryOperation.OpCode.Call, new NameOperand("printf"));
            yield return Operation.Add(Register.Rsp, 40);
            yield return new Ret();
        }

        private static (Dictionary<int, (bool onStack, int idx)>, StructType stackStruct, int usedRegs) MakeParams(
            Function function)
        {
            var regs = 0;
            var stacks = 0;
            var fields = function
                .Type
                .Params
                .Fields;
            var paramData = fields
                .Select(v => (onStack: !(SizeOf(v) <= 8 && regs++ < 4), v))
                .Select((v, i) => (typeIdx: i, v.onStack, idx: v.onStack ? stacks++ : regs - 1))
                .ToList();
            var str = new StructType(paramData.Where(v => v.onStack).Select(v => fields[v.typeIdx]).ToList());
            return (
                paramData
                    .ToDictionary(
                        v => v.typeIdx,
                        v => (v.onStack, v.onStack ? GetOffset(str, v.idx) : v.idx)
                    ),
                str, regs
            );
        }

        public IEnumerable<Operation> VisitFunction(Function function)
        {
            yield return new Label($"Function{_currentFunction}");
            _ret = new Label(".ret");
            for (var i = 0; i < CurrentFunctionParams.regs; i++)
                yield return Operation.Mov(new Memory(new Shift(Register.Rsp, -8 - i * 8), 8), Params[i]);
            foreach (var op in function.Code.AcceptVisitor(this))
                yield return op;
            yield return _ret;
            yield return new Ret();
        }

        public IEnumerable<Operation> VisitBlock(Block block)
        {
            var frameSize = RoundBy8(SizeOf(block.Variables)) +
                            _functionParams[_currentFunction].Item1.Count(vi => vi.Value.onStack) * 8;
            _functionFrame += frameSize;
            yield return Operation.Sub(Register.Rsp, frameSize);
            foreach (var op in block.Statements.SelectMany(s => s.AcceptVisitor(this)))
                yield return op;
            yield return Operation.Add(Register.Rsp, frameSize);
        }

        public IEnumerable<Operation> VisitConditional(Conditional conditional)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<Operation> VisitCycle(Cycle cycle)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<Operation> VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            var exprs = expressionStatement.Expr.AcceptVisitor(this);
            return DevirtualizeRegisters(exprs
                .Where(op => !(
                        op is BinaryOperation bo && bo.Op == BinaryOperation.OpCode.Add &&
                        bo.Right is Constant c && c.Value == 0
                    )
                )
                .ToList()
            );
        }

        public IEnumerable<Operation> VisitReturn(Return @return)
        {
            yield return Operation.Add(Register.Rsp, _functionFrame);
            _functionFrame = 0;
            yield return Operation.Jump(_ret);
        }

        public IEnumerable<Operation> AddressExpression(Expression expression)
        {
            return expression switch
            {
                BinaryExpression binaryExpression => AddressBinaryExpression(binaryExpression),
                MagicExpression magicExpression => AddressMagicExpression(magicExpression),
                NameExpression nameExpression => AddressNameExpression(nameExpression),
                UnaryExpression unaryExpression => AddressUnaryExpression(unaryExpression),
                GetFieldExpression getFieldExpression => AddressGetFieldExpression(getFieldExpression),
                _ => throw new ArgumentOutOfRangeException(nameof(expression) + $"was {expression}"),
            };
        }

        public IEnumerable<Operation> VisitMagicExpression(MagicExpression magicExpression)
            => new[] {Operation.Mov(NewRegister(8), new NameOperand(magicExpression.Name))};

        public IEnumerable<Operation> AddressMagicExpression(MagicExpression magicExpression)
            => new[] {Operation.Mov(NewRegister(8), new NameOperand(magicExpression.Name))};

        public IEnumerable<Operation> VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            foreach (var op in binaryExpression.Left.AcceptVisitor(this))
                yield return op;

            var left = LastRegister;

            foreach (var op in binaryExpression.Right.AcceptVisitor(this))
                yield return op;

            switch (binaryExpression.Operator)
            {
                case OperationKind.Add:
                    yield return Operation.Add(LastRegister, left);
                    break;
                case OperationKind.Multiply:
                    yield return Operation.Mov(Register.Rax, left);
                    yield return Operation.Mul(LastRegister);
                    yield return Operation.Mov(LastRegister, Register.Rax);
                    break;
                default:
                    throw new ArgumentException($"Unsupported operation {binaryExpression.Operator}");
            }
        }

        public IEnumerable<Operation> AddressBinaryExpression(BinaryExpression binaryExpression)
        {
            foreach (var op in binaryExpression.Right.AcceptVisitor(this))
                yield return op;
            var right = LastRegister;

            foreach (var op in AddressExpression(binaryExpression.Left))
                yield return op;
            var left = LastRegister;

            switch (binaryExpression.Operator)
            {
                case OperationKind.Add:
                    yield return Operation.Add(left, right);
                    break;
                default:
                    throw new ArgumentException(
                        $"Unsupported binary operation {binaryExpression.Operator} in calculating address");
            }
        }

        public IEnumerable<Operation> VisitGetFieldExpression(GetFieldExpression expression)
        {
            var str = (StructType) TypeOf(expression.Left);
            if (!(str.Fields[expression.Field] is NumberType nt))
                throw new Exception("Getter value calculation must result in simple type");

            if (expression.Left is NameExpression ne && ne.Depth == 1)
            {
                var (onStack, idx) = _functionParams[_currentFunction].Item1[expression.Field];
                yield return Operation.Mov(
                    NewRegister(8),
                    new Memory(new Shift(Register.Rsp, onStack ? 8 + idx : -8 * idx), nt.BitSize / 8)
                );
                yield break;
            }

            foreach (var op in AddressExpression(expression.Left))
                yield return op;
            var adr = LastRegister;

            yield return Operation.Mov(
                NewRegister(nt.BitSize / 8),
                new Memory(new Shift(adr, GetOffset(str, expression.Field)), adr.Size)
            );
        }

        public IEnumerable<Operation> AddressGetFieldExpression(GetFieldExpression expression)
        {
            if (expression.Left is NameExpression ne && ne.Depth == 1)
            {
                var (onStack, idx) = _functionParams[_currentFunction].Item1[expression.Field];
                yield return Operation.Lea(
                    NewRegister(8),
                    new Shift(Register.Rsp, onStack ? 8 + idx : -8 * idx)
                );
                yield break;
            }

            var str = (StructType) TypeOf(expression.Left);
            foreach (var op in AddressExpression(expression.Left))
                yield return op;
            yield return Operation.Add(
                LastRegister,
                GetOffset(str, expression.Field)
            );
        }

        public IEnumerable<Operation> VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            foreach (var op in unaryExpression.Right.AcceptVisitor(this))
                yield return op;
            switch (unaryExpression.Operator)
            {
                default:
                    throw new ArgumentException($"Unsupported operation {unaryExpression.Operator}");
            }
        }

        public IEnumerable<Operation> AddressUnaryExpression(UnaryExpression unaryExpression)
        {
            return unaryExpression.Operator switch
            {
                OperationKind.Dereference => unaryExpression.Right.AcceptVisitor(this),
                _ => throw new ArgumentException(
                    $"Unsupported unary operation {unaryExpression.Operator} in calculating address")
            };
        }

        public IEnumerable<Operation> VisitAssignExpression(AssignExpression assignExpression)
        {
            var type = TypeOf(assignExpression.Right);
            foreach (var op in AddressExpression(assignExpression.Left))
                yield return op;
            var destAdr = LastRegister;

            if (type is NumberType || type is PointerType)
            {
                foreach (var op in assignExpression.Right.AcceptVisitor(this))
                    yield return op;
                yield return Operation.Mov(new Memory(destAdr, SizeOf(type)), LastRegister);
                yield break;
            }

            foreach (var op in AddressExpression(assignExpression.Right))
                yield return op;
            var srcAdr = LastRegister;

            foreach (var op in MakeAssigner(new Shift(destAdr, 0), new Shift(srcAdr, 0), type))
                yield return op;
        }

        public IEnumerable<Operation> MakeAssigner(Shift left, Shift right, BaseType type)
        {
            switch (type)
            {
                case EmptyType _:
                    break;
                case FunctionType _:
                    throw new Exception("Can not assign functions");
                case NumberType numberType:
                {
                    var r = NewRegister(numberType.BitSize / 8);
                    yield return Operation.Mov(r, new Memory(right, numberType.BitSize / 8));
                    yield return Operation.Mov(new Memory(left, numberType.BitSize / 8), r);
                    break;
                }
                case PointerType _:
                {
                    var r = NewRegister(8);
                    yield return Operation.Mov(r, new Memory(right, 8));
                    yield return Operation.Mov(new Memory(left, 8), r);
                    break;
                }
                case StructType structType:
                    foreach (var op in structType.Fields.SelectMany((f, i) =>
                        MakeAssigner(
                            new Shift(left.Register, left.Shft + GetOffset(structType, i)),
                            new Shift(right.Register, right.Shft + GetOffset(structType, i)),
                            f
                        )
                    ))
                        yield return op;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static List<AnyRegister> Params = new List<AnyRegister>
        {
            Register.Rcx, Register.Rdx, Register.R8, Register.R9,
        };

        public IEnumerable<Operation> VisitCallExpression(CallExpression callExpression)
        {
            var results = new List<AnyRegister>();
            foreach (var ops in callExpression.Params.Select(p => p.AcceptVisitor(this)))
            {
                foreach (var op in ops)
                    yield return op;
                results.Add(LastRegister);
            }

            foreach (var op in callExpression.Function.AcceptVisitor(this))
                yield return op;
            var func = LastRegister;
            foreach (var (res, i) in results.Select((v, i) => (v, i)))
                yield return Operation.Mov(Params[i], res);
            yield return new UnaryOperation(UnaryOperation.OpCode.Call, func);
        }

        public IEnumerable<Operation> VisitConstExpression(ConstExpression constExpression)
        {
            yield return Operation.Mov(NewRegister(8), new Constant((int) constExpression.Value));
        }

        public IEnumerable<Operation> VisitNameExpression(NameExpression nameExpression)
        {
            throw new Exception("Cannot get value of ");
        }

        public IEnumerable<Operation> AddressNameExpression(NameExpression nameExpression)
        {
            yield return nameExpression.Depth switch
            {
                0 => throw new Exception("Cannot ger address of global scope"),
                1 => Operation.Mov(NewRegister(8), new Shift(Register.Rsp, 8)),
                2 => Operation.Lea(NewRegister(8), new Shift(Register.Rsp, -_functionFrame)),
                _ => throw new Exception($"Unknown scope {nameExpression.Depth}")
            };
        }

        private readonly Dictionary<object, BaseType> _typeCache = new Dictionary<object, BaseType>();

        private BaseType TypeOf(Expression expression)
        {
            if (_typeCache.ContainsKey(expression))
                return _typeCache[expression];
            BaseType result;
            switch (expression)
            {
                case AssignExpression _:
                    result = new EmptyType();
                    break;
                case BinaryExpression binaryExpression:
                {
                    var l = TypeOf(binaryExpression.Left);
                    var r = TypeOf(binaryExpression.Right);
                    result = binaryExpression.Operator switch
                    {
                        OperationKind.Add => VisitNumericOperation(l, r,
                            $"{binaryExpression.Left} and {binaryExpression.Right} are not valid addition operands"),
                        OperationKind.Multiply => VisitNumericOperation(l, r,
                            $"{binaryExpression.Left} and {binaryExpression.Right} are not valid multiplication operands"),
                    };
                    break;
                }
                case CallExpression callExpression:
                {
                    var func = TypeOf(callExpression.Function);
                    // TODO
                    var args = callExpression.Params.Select(TypeOf).ToList();
                    if (!(func is FunctionType f))
                        throw new Exception("Function must be callable");
                    result = f.Result;
                    break;
                }
                case ConstExpression constExpression:
                    result = constExpression.Type;
                    break;
                case MagicExpression _:
                    result = new EmptyType();
                    break;
                case NameExpression nameExpression:
                    result = nameExpression.Depth switch
                    {
                        1 => (_module.Entities[_currentFunction] as Function)?.Type.Params,
                        2 => (_module.Entities[_currentFunction] as Function)?.Code.Variables,
                        _ => throw new ArgumentException(),
                    };
                    break;
                case UnaryExpression unaryExpression:
                {
                    var r = TypeOf(unaryExpression.Right);
                    result = unaryExpression.Operator switch
                    {
                        OperationKind.Dereference => r is PointerType pt
                            ? pt.To
                            : throw new Exception("Invalid operand for dereferencing"),
                        _ => throw new ArgumentException(),
                    };
                    break;
                }
                case GetFieldExpression getFieldExpression:
                {
                    var field = getFieldExpression.Field;
                    var left = getFieldExpression.Left;
                    if (left is NameExpression ne && ne.Depth == 0)
                        return (_module.Entities[field] as Function)?.Type;
                    if (TypeOf(left) is StructType st)
                        return st.Fields[field];
                    throw new Exception($"Cannot get field {field} of struct {left}");
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }

            _typeCache[expression] = result;
            return result;
        }

        private BaseType VisitNumericOperation(BaseType l, BaseType r, string text)
        {
            return l is NumberType lN && r is NumberType rN &&
                   lN.NumberKind == rN.NumberKind && lN.BitSize == rN.BitSize
                ? l
                : throw new Exception(text);
        }

        private static int GetOffset(StructType str, int i)
        {
            var varSizes = str.Fields.Select(SizeOf).ToList();
            var lastChunk = 0;
            var offset = 0;
            foreach (var size in varSizes.Take(i))
            {
                if (size <= lastChunk)
                    offset += size;
                else
                    offset = RoundBy8(offset) + size;
                lastChunk = size;
            }

            return offset;
        }

        private static int SizeOf(BaseType type)
        {
            return type switch
            {
                ArrayType arrayType => SizeOf(arrayType.ElemType) * arrayType.Length,
                EmptyType _ => 0,
                FunctionType _ => 0,
                NumberType numberType => numberType.BitSize / 8,
                PointerType _ => 8,
                StructType structType => SizeOfStruct(structType),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static int SizeOfStruct(StructType str)
        {
            var varSizes = str.Fields.Select(SizeOf).ToList();
            var lastChunk = 0;
            var offset = 0;
            foreach (var size in varSizes)
            {
                if (size <= lastChunk)
                    offset += size;
                else
                    offset = RoundBy8(offset) + size;
                lastChunk = size;
            }

            return offset;
        }

        private static int RoundBy8(int val)
        {
            if (val % 8 == 0)
                return val;
            return val / 8 + 1;
        }

        public static List<Register.RegisterId> registers = new List<Register.RegisterId>
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

        private static IEnumerable<Operation> DevirtualizeRegisters(List<Operation> code)
        {
            var ru = new RegisterUsage(code);
            var replace = new Dictionary<int, Register.RegisterId>();
            foreach (var id in ru.PrioritisedRegisters())
            {
                var n = ru.CanNotReplace(id);
                var to = registers.First(r => !n.Contains(RegisterUsage.ToReg(r)));
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
    }

    public class RegisterUsage
    {
        private readonly Dictionary<int, HashSet<int>> _uses = new Dictionary<int, HashSet<int>>();

        public RegisterUsage(IEnumerable<Operation> code)
        {
            foreach (var (op, i) in code.Select((v, i) => (v, i)))
            {
                if (op is BinaryOperation binaryOperation)
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
                }

                else if (op is UnaryOperation unaryOperation)
                {
                    var used = UsedRegisters(unaryOperation.Right);
                    if (used != -1)
                    {
                        if (!_uses.ContainsKey(used))
                            _uses[used] = new HashSet<int>();
                        _uses[used].Add(i);
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

        public HashSet<int> UsedRegisters(int line)
            => _uses.Where(kv => kv.Value.Contains(line)).Select(kv => kv.Key).ToHashSet();

        public IEnumerable<int> PrioritisedRegisters()
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