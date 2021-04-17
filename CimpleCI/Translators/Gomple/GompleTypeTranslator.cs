using System;
using System.Collections.Generic;
using System.Linq;
using Frontend;
using Frontend.Lexer;

namespace CimpleCI.Translators.Gomple
{
    public class GompleTypeTranslator
    {
        private readonly Dictionary<string, GompleAst.Type> NamedTypes = new Dictionary<string, GompleAst.Type>();

        private readonly Dictionary<(string name, GompleAst.Type receiver), GompleAst.Type> MethodSignatures =
            new Dictionary<(string name, GompleAst.Type receiver), GompleAst.Type>();

        private void AddMagic()
        {
            AddName("Print", new GompleAst.FunctionType
            {
                Args = new GompleAst.StructType
                {
                    Variables = new List<GompleAst.Variable>
                    {
                        new GompleAst.Variable
                        {
                            Name = new Token(0, "value", 0),
                            Type = new GompleAst.IntegerType(),
                        },
                    }
                },
                Result = new GompleAst.VoidType()
            });
        }

        public TypedGompleAst.Program VisitProgram(GompleAst.Program program)
        {
            foreach (var typeDef in program.Types)
                NamedTypes[typeDef.Name.Text] = typeDef.Type;
            PushLayer();

            AddMagic();
            foreach (var function in program.Functions)
                switch (function.Type)
                {
                    case GompleAst.MethodType met:
                        if (FindMethod(met.Sender.Type, function.Name.Text) != null)
                            throw new Exception($"Method {function.Name.Text} is duplicated");
                        MethodSignatures[(function.Name.Text, met.Sender.Type)] = function.Type;
                        break;
                    case GompleAst.FunctionType func:
                        AddName(function.Name.Text, function.Type);
                        break;
                }

            var result = new TypedGompleAst.Program
            {
                Functions = program.Functions.Select(VisitFunction).ToList(),
                NamedTypes = NamedTypes,
                Methods = MethodSignatures,
            };

            PopLayer();

            return result;
        }

        private TypedGompleAst.Function VisitFunction(GompleAst.Function function)
        {
            PushLayer();
            foreach (var variable in function.Type.Args.Variables)
                AddName(variable.Name.Text, variable.Type);
            if (function.Type is GompleAst.MethodType met)
            {
                AddName(met.Sender.Name.Text, met.Sender.Type);
            }

            var result = new TypedGompleAst.Function
            {
                Type = function.Type,
                Name = function.Name,
                Body = VisitBlock(function.Body),
            };
            PopLayer();
            return result;
        }

        private TypedGompleAst.Block VisitBlock(GompleAst.Block block)
        {
            PushLayer();
            var result = new TypedGompleAst.Block
            {
                Statements = block.Statements.Select(VisitStatement).ToList(),
            };
            PopLayer();
            return result;
        }

        private TypedGompleAst.Statement VisitStatement(GompleAst.Statement statement)
            => statement switch
            {
                GompleAst.AssignStatement assign => (TypedGompleAst.Statement) new TypedGompleAst.AssignStatement
                {
                    Left = VisitExpression(assign.Left),
                    Right = VisitExpression(assign.Right),
                },
                GompleAst.ExpressionStatement expression => new TypedGompleAst.ExpressionStatement
                {
                    Expression = VisitExpression(expression.Expression),
                },
                GompleAst.ReturnStatement ret => new TypedGompleAst.ReturnStatement
                {
                    Value = VisitExpression(ret.Value),
                },
                GompleAst.ReturnNothingStatement _ => new TypedGompleAst.ReturnNothingStatement(),
                GompleAst.VarAssignStatement varAssign => new TypedGompleAst.VarAssignStatement
                {
                    Name = AddToken(varAssign.Name, varAssign.Type),
                    Type = varAssign.Type,
                    Value = VisitExpression(varAssign.Value),
                },
                GompleAst.VarStatement varStmt => new TypedGompleAst.VarStatement
                {
                    Name = AddToken(varStmt.Name, varStmt.Type),
                    Type = varStmt.Type,
                },
                _ => throw new ArgumentOutOfRangeException(nameof(statement))
            };

        private List<Dictionary<string, GompleAst.Type>> stack = new List<Dictionary<string, GompleAst.Type>>();

        private void PushLayer()
            => stack.Add(new Dictionary<string, GompleAst.Type>());

        private void PopLayer()
            => stack.RemoveAt(stack.Count - 1);

        private void AddName(string name, GompleAst.Type type)
            => stack[^1][name] = type;

        private Token AddToken(Token token, GompleAst.Type type)
        {
            AddName(token.Text, type);
            return token;
        }

        private GompleAst.Type FindName(string name)
            => stack
                   .AsEnumerable()
                   .FirstOrDefault(d => d.ContainsKey(name))?[name] ??
               throw new Exception($"{name} not found");

        private TypedGompleAst.Expression VisitExpression(GompleAst.Expression expression)
        {
            switch (expression)
            {
                case GompleAst.BinExpression bin:
                {
                    var left = VisitExpression(bin.Left);
                    var right = VisitExpression(bin.Right);
                    if (!left.Type.Equals(right.Type))
                        throw new Exception($"Type mismatch {left.Type} != {right.Type}");
                    return new TypedGompleAst.BinExpression
                    {
                        Left = left,
                        Right = right,
                        Op = bin.Op,
                        Type = left.Type,
                    };
                }
                case GompleAst.CallExpression call:
                {
                    var func = VisitExpression(call.Function);
                    var pars = call.Params.Select(VisitExpression).ToList();
                    if (!(func.Type is GompleAst.CallableType cFunc))
                        throw new Exception($"Can not call {func.Type}");
                    if (cFunc.Args.Variables.Select(v => v.Type).SequenceEqual(pars.Select(p => p.Type)))
                        return new TypedGompleAst.CallExpression
                        {
                            Type = cFunc.Result,
                            Function = func,
                            Params = pars,
                        };
                    throw new Exception($"Type mismatch {cFunc.Args} != {pars}");
                }
                case GompleAst.NameExpression name:
                    return new TypedGompleAst.NameExpression
                    {
                        Type = FindName(name.Name.Text),
                        Name = name.Name,
                    };
                case GompleAst.GetExpression get:
                    return VisitGetExpression(get);
                case GompleAst.IntegerConstExpression i:
                    return new TypedGompleAst.IntegerConstExpression
                    {
                        Type = new GompleAst.IntegerType(),
                        Value = i.Value
                    };
                case GompleAst.FloatConstExpression f:
                    return new TypedGompleAst.FloatConstExpression
                    {
                        Type = new GompleAst.FloatType(),
                        Value = f.Value
                    };
                default:
                    throw new ArgumentException();
            }
        }

        private TypedGompleAst.Expression VisitGetExpression(GompleAst.GetExpression get, int getter = 0)
        {
            if (getter == get.Field.Count)
            {
                var left = VisitExpression(get.Struct);
                return left;
            }
            
            var getLeft = VisitGetExpression(get, getter + 1);
            
            var t = getLeft.Type;

            var getterIdx = get.Field.Count - getter - 1;
            
            var mth = FindMethod(t, get.Field[getterIdx].Field.Text);
            if (mth != null)
                return new TypedGompleAst.GetExpression
                {
                    Type = mth,
                    Struct = VisitGetExpression(get, getter + 1),
                    Field = get.Field[getterIdx].Field,
                };


            while (t is GompleAst.PointerType || t is GompleAst.TypeRef)
            {
                while (t is GompleAst.PointerType pt)
                    t = pt.To;
                if (t is GompleAst.TypeRef)
                    t = UnrefType(t);
            }
            
            if (!(t is GompleAst.StructType str))
                throw new ArgumentException($"{t} is not struct");

            var type = str
                .Variables
                .Select((v, i) => (v, i))
                .First(vi => vi.v.Name.Text == get.Field[getterIdx].Field.Text)
                .v
                .Type;

            return new TypedGompleAst.GetExpression
            {
                Type = type,
                Struct = VisitGetExpression(get, getter + 1),
                Field = get.Field[getterIdx].Field,
            };
        }

        private GompleAst.Type FindMethod(GompleAst.Type receiver, string name)
        {
            if (MethodSignatures.ContainsKey((name, receiver)))
                return MethodSignatures[(name, receiver)];

            while (receiver is GompleAst.PointerType pt)
            {
                receiver = pt.To;
                if (MethodSignatures.ContainsKey((name, receiver)))
                    return MethodSignatures[(name, receiver)];
            }

            return null;
        }

        private GompleAst.Type UnrefType(GompleAst.Type typeRef)
        {
            while (typeRef is GompleAst.TypeRef t)
                typeRef = NamedTypes[t.Name.Text];

            return typeRef;
        }
    }
}