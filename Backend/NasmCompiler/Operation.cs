using System.Reflection.PortableExecutable;

namespace Backend.NasmCompiler
{
    public abstract class Operation
    {
        public static Operation Sub(Operand left, Operand right)
            => new BinaryOperation(BinaryOperation.OpCode.Sub, left, right);

        public static Operation Sub(Operand left, int right)
            => Sub(left, new Constant(right));

        public static Operation Add(Operand left, Operand right)
            => new BinaryOperation(BinaryOperation.OpCode.Add, left, right);

        public static Operation Add(Operand left, int right)
            => Add(left, new Constant(right));

        public static Operation Jump(Label l)
            => new JumpOperation(l, JumpOperation.JumpCondition.Always);

        public static Operation Mov(Operand left, Operand right)
            => new BinaryOperation(BinaryOperation.OpCode.Mov, left, right);

        public static Operation Lea(Operand left, Operand right)
            => new BinaryOperation(BinaryOperation.OpCode.Lea, left, right);

        public static Operation Mul(Operand right)
            => new UnaryOperation(UnaryOperation.OpCode.Mul, right);
    }

    public class BinaryOperation : Operation
    {
        public OpCode Op;
        public Operand Left;
        public Operand Right;

        public BinaryOperation(OpCode op, Operand left, Operand right)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        public enum OpCode
        {
            Add,
            Sub,
            Mov,
            Lea,
        }

        public override string ToString()
            => Op == OpCode.Lea ? $"{Op}\t{Left},\t[{Right}]" : $"{Op}\t{Left},\t{Right}";
    }

    public class UnaryOperation : Operation
    {
        public OpCode Op;

        public Operand Right;

        public UnaryOperation(OpCode op, Operand right)
        {
            Op = op;
            Right = right;
        }

        public enum OpCode
        {
            Push,
            Pop,
            Mul,
            Call,
        }

        public override string ToString()
            => $"{Op}\t{Right}";
    }

    public class Ret : Operation
    {
        public override string ToString()
            => "ret";
    }

    public class Label : Operation
    {
        public string Name;

        public Label(string name)
        {
            Name = name;
        }

        public override string ToString()
            => $"\t{Name}:";
    }

    public class JumpOperation : Operation
    {
        public Label Where;

        public JumpCondition Condition;

        public JumpOperation(Label where, JumpCondition condition)
        {
            Where = where;
            Condition = condition;
        }

        public enum JumpCondition
        {
            Less,
            LessOrEqual,
            Equal,
            NotEqual,
            GreaterOrEqual,
            Greater,
            Always,
        }

        public override string ToString()
            => $"JMP {Where.Name}";
    }

    public class AsmLine : Operation
    {
        public string Line;

        public AsmLine(string line)
        {
            Line = line;
        }

        public override string ToString()
            => Line;
    }
}