using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.AST;

namespace Frontend
{
    public class RuleCallback
    {
        public abstract class Instruction
        {
            
        }

        public class Return : Instruction
        {
            private readonly Expr _value;

            public Return(Expr value)
            {
                _value = value;
            }

            public IASTNode Eval(List<IASTNode> args)
                => _value.Eval(args);

            public override string ToString()
                => $"return {_value};";
        }

        public class Add : Instruction
        {
            private readonly Getter _list;
            private readonly Expr _value;

            public Add(Getter list, Expr value)
            {
                _list = list;
                _value = value;
            }
            
            public void Eval(List<IASTNode> args)
                => ((ASTList)_list.Eval(args)).Insert(_value.Eval(args));

            public override string ToString()
                => $"{_list} <- {_value};";
            
        }

        public abstract class Expr
        {
            public abstract IASTNode Eval(List<IASTNode> args);
        }

        public class Getter : Expr
        {
            private readonly int _arg;
            private readonly List<string> _fields;

            public Getter(int arg, List<string> fields)
            {
                _arg = arg;
                _fields = fields;
            }

            public override IASTNode Eval(List<IASTNode> args)
                => _fields.Aggregate(args[_arg], (current, field) => current[field]);

            public override string ToString()
                => $"${_arg}.{string.Join(".", _fields)}";
            
        }

        public class Construction : Expr
        {
            private readonly IPrototype _type;
            private readonly List<Expr> _args;

            public Construction(IPrototype type, List<Expr> args)
            {
                _type = type;
                _args = args;
            }

            public override IASTNode Eval(List<IASTNode> args)
                =>_type.New(_args.Select(e => e.Eval(args)).ToArray());

            public override string ToString()
                => $"{_type.Name()}({string.Join(", ", _args.Select(a => a.ToString()))})";
        }

        private readonly List<Instruction> _program;

        public RuleCallback(List<Instruction> program)
        {
            this._program = program;
        }

        public IASTNode Call(List<IASTNode> list)
        {
            foreach (var instruction in _program)
            {
                switch (instruction)
                {
                    case Return ret:
                        return ret.Eval(list);
                    case Add add:
                        add.Eval(list);
                        break;
                }
            }

            throw new Exception("There was no return Instruction");
        }

        public override string ToString()
            => string.Join("\n", _program.Select(i => i.ToString()));
    }
}