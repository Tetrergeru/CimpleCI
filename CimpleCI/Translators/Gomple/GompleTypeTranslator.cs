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

        public TypedGompleAst.Program VisitProgram(GompleAst.Program program)
        {
            foreach (var typeDef in program.Types)
                NamedTypes[typeDef.Name.Text] = typeDef.Type;
            PushLayer();

            foreach (var function in program.Functions)
                switch (function.Type)
                {
                    case GompleAst.MethodType met:
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
                AddName(met.Sender.Name.Text, met.Sender.Type);

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
            => stack.AsEnumerable().Reverse().FirstOrDefault(d => d.ContainsKey(name))?[name] ?? throw new Exception($"{name} not found");

        private TypedGompleAst.Expression VisitExpression(GompleAst.Expression expression)
        {
            switch (expression)
            {
                case GompleAst.BinExpression bin:
                {
                    var left = VisitExpression(bin.Left);
                    var right = VisitExpression(bin.Right);
                    if (left.Type != right.Type)
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
                {
                    var left = VisitExpression(get.Struct);

                    if (MethodSignatures.ContainsKey((get.Field.Text, left.Type)))
                        return new TypedGompleAst.GetExpression
                        {
                            Type = MethodSignatures[(get.Field.Text, left.Type)],
                            Struct = left,
                            Field = get.Field,
                        };

                    var t = left.Type;
                    if (t is GompleAst.TypeRef)
                        t = UnrefType(t);

                    if (!(t is GompleAst.StructType str))
                        throw new ArgumentException();

                    var type = str.Variables.Select((v, i) => (v, i)).First(vi => vi.v.Name.Text == get.Field.Text)
                        .v.Type;
                    return new TypedGompleAst.GetExpression
                    {
                        Type = type,
                        Struct = left,
                        Field = get.Field,
                    };
                }
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

        private GompleAst.Type UnrefType(GompleAst.Type typeRef)
        {
            while (typeRef is GompleAst.TypeRef t)
                typeRef = NamedTypes[t.Name.Text];

            return typeRef;
        }
    }
}