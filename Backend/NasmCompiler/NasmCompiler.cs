using System;
using System.Collections.Generic;
using System.Linq;
using Middleend;
using Middleend.Expressions;
using Middleend.Statements;
using Middleend.Types;

namespace Backend.NasmCompiler
{
    public class NasmCompiler : IModuleVisitor<IEnumerable<Operation>>
    {
        private ExpressionAddressVisitor _addressVisitor;

        public TypeEvaluator Types;

        public Module Module;

        private int _usedRegisters;

        public AnyRegister NewRegister(int size) => LastRegister = new StubRegister(_usedRegisters++, size);

        public AnyRegister LastRegister { get; private set; }

        public int CurrentFunction;

        public Dictionary<FunctionType, FunctionData> FunctionParams;

        public FunctionData CurrentFunctionParams =>
            FunctionParams[((Function) Module.Entities[CurrentFunction]).Type];

        public int FunctionFrame;

        public int LocalParams;

        public int LocalVariables;

        private Label _ret;

        public IEnumerable<Operation> VisitModule(Module module)
        {
            _addressVisitor = new ExpressionAddressVisitor(this);
            Types = new TypeEvaluator(this);
            yield return new AsmLine("extern printf");
            yield return new AsmLine("extern ExitProcess");
            yield return new AsmLine("section .text");
            yield return new AsmLine("global Start");
            yield return new AsmLine("Start: jmp Function0");

            FunctionParams = module
                .Entities
                .Select(v => ((Function) v).Type)
                .Distinct()
                .ToDictionary(v => v, v => new FunctionData(v, this));

            foreach (var op in HardCode())
                yield return op;

            Module = module;
            foreach (var e in module.Entities)
            {
                foreach (var op in e.AcceptVisitor(this))
                    yield return op;
                CurrentFunction++;
            }

            yield return new AsmLine("section .data");
            yield return new AsmLine("fmt:    db \"%d\", 10, 0");
        }

        private IEnumerable<Operation> HardCode()
        {
            var printType = new FunctionType(new StructType(new NumberType(NumberKind.SignedInteger, 64)),
                new EmptyType());
            FunctionParams[printType] = new FunctionData(printType, this);
            yield return new Label("Print");
            yield return Operation.Mov(Register.Rdx, Register.Rcx);
            yield return Operation.Mov(Register.Rcx, new NameOperand("fmt"));
            yield return Operation.Sub(Register.Rsp, 40);
            yield return new UnaryOperation(UnaryOperation.OpCode.Call, new NameOperand("printf"));
            yield return Operation.Add(Register.Rsp, 40);
            yield return new Ret();
        }

        public IEnumerable<Operation> VisitFunction(Function function)
        {
            yield return new Label($"Function{CurrentFunction}");
            _ret = new Label(".ret");

            LocalParams = CurrentFunctionParams.Registers * 8;
            FunctionFrame += LocalParams;

            yield return Operation.Sub(Register.Rsp, LocalParams);

            for (var i = 0; i < CurrentFunctionParams.Registers; i++)
                yield return Operation.Mov(new Memory(new Shift(Register.Rsp, -i * 8), 8), Params[i]);

            foreach (var op in function.Code.AcceptVisitor(this))
                yield return op;

            yield return _ret;

            FunctionFrame -= LocalParams;
            yield return Operation.Add(Register.Rsp, LocalParams);

            if (CurrentFunction == 0)
            {
                yield return new AsmLine("xor rcx, rcx");
                yield return new AsmLine("call ExitProcess");
            }
            else
                yield return new Ret();
        }

        public IEnumerable<Operation> VisitBlock(Block block)
        {
            LocalVariables = TypeEvaluator.RoundBy8(TypeEvaluator.SizeOf(block.Variables));

            FunctionFrame += LocalVariables;
            yield return Operation.Sub(Register.Rsp, LocalVariables);

            foreach (var op in block.Statements.SelectMany(s => s.AcceptVisitor(this)))
                yield return op;

            yield return Operation.Add(Register.Rsp, LocalVariables);
            FunctionFrame -= LocalVariables;
        }

        public IEnumerable<Operation> VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            var expressions = expressionStatement.Expr.AcceptVisitor(this);
            return RegisterDevirtualizer.DevirtualizeRegisters(expressions
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
            yield return Operation.Add(Register.Rsp, FunctionFrame);
            FunctionFrame = 0;
            yield return Operation.Jump(_ret);
        }


        public IEnumerable<Operation> VisitMagicExpression(MagicExpression magicExpression)
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


        public int ParameterOffset()
            => FunctionFrame;

        public int ParameterOffset(bool onStack, int idx)
            => ParameterOffset() + (onStack ? 8 + idx : -8 * (idx + 1));

        public int VariableOffset()
            => FunctionFrame - LocalParams - LocalVariables;

        public int VariableOffset(StructType str, int idx)
            => VariableOffset() + TypeEvaluator.GetOffset(str, idx);

        public IEnumerable<Operation> VisitGetFieldExpression(GetFieldExpression expression)
        {
            if (expression.Left is NameExpression ne0 && ne0.Depth == 0)
            {
                yield return Operation.Mov(
                    NewRegister(8),
                    new NameOperand($"Function{expression.Field}")
                );
                yield break;
            }

            var str = (StructType) Types.TypeOf(expression.Left);
            var size = str.Fields[expression.Field] switch
            {
                NumberType nt => (nt.BitSize / 8),
                PointerType _ => 8,
                _ => throw new Exception("Getter value calculation must result in simple type")
            };

            if (expression.Left is NameExpression ne && ne.Depth == 1)
            {
                var (onStack, idx) = CurrentFunctionParams.Params[expression.Field];
                yield return new AsmLine("; here 1");
                yield return Operation.Mov(
                    NewRegister(8),
                    new Memory(new Shift(Register.Rsp, ParameterOffset(onStack, idx)), size)
                );
                yield return new AsmLine("; here 2");
                yield break;
            }

            foreach (var op in _addressVisitor.VisitExpression(expression.Left))
                yield return op;
            var adr = LastRegister;

            yield return Operation.Mov(
                NewRegister(size),
                new Memory(new Shift(adr, VariableOffset(str, expression.Field)), adr.Size)
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

        public IEnumerable<Operation> VisitAssignExpression(AssignExpression assignExpression)
        {
            var type = Types.TypeOf(assignExpression.Right);
            foreach (var op in _addressVisitor.VisitExpression(assignExpression.Left))
                yield return op;
            var destAdr = LastRegister;

            if (type is NumberType || type is PointerType)
            {
                foreach (var op in assignExpression.Right.AcceptVisitor(this))
                    yield return op;
                yield return Operation.Mov(new Memory(destAdr, TypeEvaluator.SizeOf(type)), LastRegister);
                yield break;
            }

            foreach (var op in _addressVisitor.VisitExpression(assignExpression.Right))
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
                            new Shift(left.Register, left.Shft + TypeEvaluator.GetOffset(structType, i)),
                            new Shift(right.Register, right.Shft + TypeEvaluator.GetOffset(structType, i)),
                            f
                        )
                    ))
                        yield return op;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static readonly List<AnyRegister> Params = new List<AnyRegister>
        {
            Register.Rcx, Register.Rdx, Register.R8, Register.R9,
        };

        public IEnumerable<Operation> VisitCallExpression(CallExpression callExpression)
        {
            var type = (FunctionType) Types.TypeOf(callExpression.Function);
            var pars = FunctionParams[type];
            var
                offset = TypeEvaluator.RoundBy8(
                    TypeEvaluator.SizeOf(pars.StackStruct)); //pars.StackStruct.Fields.Count == 0 ? 0 : 0; //
            FunctionFrame += offset;
            yield return Operation.Sub(Register.Rsp, offset);

            var registerResults = new List<AnyRegister>();
            var arguments = callExpression.Params;
            for (var i = 0; i < arguments.Count; i++)
            {
                var (onStack, place) = pars.Params[i];
                if (onStack)
                {
                    foreach (var op in _addressVisitor.VisitExpression(arguments[i]))
                        yield return op;
                    var adr = LastRegister;

                    foreach (var op in MakeAssigner(
                            new Shift(Register.Rsp, place),
                            new Shift(adr, 0),
                            Types.TypeOf(arguments[i])
                        )
                    )
                        yield return op;
                }
                else
                {
                    foreach (var op in arguments[i].AcceptVisitor(this))
                        yield return op;
                    registerResults.Add(LastRegister);
                }
            }

            foreach (var op in callExpression.Function.AcceptVisitor(this))
                yield return op;
            var func = LastRegister;


            foreach (var (res, i) in registerResults.Select((v, i) => (v, i)))
                yield return Operation.Mov(Params[i], res);
            yield return new UnaryOperation(UnaryOperation.OpCode.Call, func);

            FunctionFrame -= offset;
            yield return Operation.Add(Register.Rsp, offset);
        }

        public IEnumerable<Operation> VisitConstExpression(ConstExpression constExpression)
        {
            yield return Operation.Mov(NewRegister(8), new Constant((int) constExpression.Value));
        }

        public IEnumerable<Operation> VisitNameExpression(NameExpression nameExpression)
        {
            throw new Exception("Cannot get value of ");
        }
    }
}