using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Middleend.Statements;

namespace Backend.NasmCompiler
{
    public class Operand
    {
    }

    public class Shift : Operand
    {
        public AnyRegister Register;
        public int Shft;

        public Shift(AnyRegister register, int shift)
        {
            Register = register;
            Shft = shift;
        }

        private static char Sign(int val) => val < 0 ? '-' : '+';

        public override string ToString()
            => $"{Register} {Sign(Shft)} {Math.Abs(Shft)}";
    }

    public abstract class AnyRegister : Operand
    {
        public int Size;
    }

    public class StubRegister : AnyRegister
    {
        public int Number;

        public StubRegister(int number, int size)
        {
            Number = number;
            Size = size;
        }

        public override string ToString()
            => $"#{Number}";
    }

    public class Memory : Operand
    {
        public int Size;

        public Operand Address;

        public Memory(Operand address, int size)
        {
            Address = address;
            Size = size;
        }

        private static readonly Dictionary<int, string> Sizes = new Dictionary<int, string>
        {
            [8] = "qword",
            [4] = "dword",
            [2] = "word",
            [1] = "byte",
        };

        public override string ToString()
            => $"{Sizes[Size]} [{Address}]";
    }

    public class Register : AnyRegister
    {
        public RegisterId Id;

        public Register(RegisterId id, int size)
        {
            Id = id;
            Size = size;
        }

        public static Register
            Rax = new Register(RegisterId.A, 8),
            Rcx = new Register(RegisterId.C, 8),
            Rdx = new Register(RegisterId.D, 8),
            Rbx = new Register(RegisterId.B, 8),
            Rsp = new Register(RegisterId.Sp, 8),
            Rbp = new Register(RegisterId.Bp, 8),
            Rsi = new Register(RegisterId.Si, 8),
            Rdi = new Register(RegisterId.Di, 8),
            R8 = new Register(RegisterId.R8, 8),
            R9 = new Register(RegisterId.R9, 8),
            R10 = new Register(RegisterId.R10, 8),
            R11 = new Register(RegisterId.R11, 8),
            R12 = new Register(RegisterId.R12, 8),
            R13 = new Register(RegisterId.R13, 8),
            R14 = new Register(RegisterId.R14, 8),
            R15 = new Register(RegisterId.R15, 8);

        public static Dictionary<(RegisterId id, int size), string> _names =
            new Dictionary<(RegisterId id, int size), string>
            {
                [(RegisterId.A, 8)] = "rax",
                [(RegisterId.B, 8)] = "rbx",
                [(RegisterId.C, 8)] = "rcx",
                [(RegisterId.D, 8)] = "rdx",
                [(RegisterId.Sp, 8)] = "rsp",
                [(RegisterId.Bp, 8)] = "rbp",
                [(RegisterId.Si, 8)] = "rsi",
                [(RegisterId.Di, 8)] = "rdi",
                [(RegisterId.R8, 8)] = "r8",
                [(RegisterId.R9, 8)] = "r9",
                [(RegisterId.R10, 8)] = "r10",
                [(RegisterId.R11, 8)] = "r11",
                [(RegisterId.R12, 8)] = "r12",
                [(RegisterId.R13, 8)] = "r13",
                [(RegisterId.R14, 8)] = "r14",
                [(RegisterId.R15, 8)] = "r15",
            };

        public enum RegisterId
        {
            A,
            B,
            C,
            D,
            Sp,
            Bp,
            Si,
            Di,
            R8,
            R9,
            R10,
            R11,
            R12,
            R13,
            R14,
            R15,
        }

        public override string ToString()
            => $"{_names[(Id, Size)]}";
    }

    public class Constant : Operand
    {
        public long Value;

        public Constant(long value)
        {
            Value = value;
        }

        public override string ToString()
            => $"{Value}";
    }

    public class NameOperand : Operand
    {
        public string Name;

        public NameOperand(string name)
        {
            Name = name;
        }

        public override string ToString()
            => $"{Name}";
    }
}