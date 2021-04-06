using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.Lexer;
using Middleend;
using Middleend.Expressions;
using Middleend.Statements;
using Middleend.Types;

namespace CimpleCI.Translators.Gomple
{
    public class TypeMapper
    {
        private readonly Dictionary<GompleAst.Type, BaseType>
            _typeMappings = new Dictionary<GompleAst.Type, BaseType>();

        private readonly BaseType _f64 = new NumberType(NumberKind.Float, 64);

        private readonly BaseType _i64 = new NumberType(NumberKind.SignedInteger, 64);

        private readonly Dictionary<string, GompleAst.Type> _namedTypes;

        public TypeMapper(TypedGompleAst.Program program)
            => _namedTypes = program.NamedTypes;

        public BaseType AddType(GompleAst.Type type)
        {
            if (_typeMappings.ContainsKey(type))
                return _typeMappings[type];
            var t = type switch
            {
                GompleAst.FunctionType functionType => new FunctionType(
                    new StructType(functionType.Args.Variables.Select(v => AddType(v.Type)).ToList()),
                    AddType(functionType.Result)
                ),
                GompleAst.MethodType methodType => new FunctionType(
                    new StructType(
                        new[] {methodType.Sender.Type}
                            .Concat(methodType.Args.Variables.Select(v => v.Type))
                            .Select(AddType)
                            .ToList()
                    ),
                    AddType(methodType.Result)
                ),
                GompleAst.IntegerType _ => _i64,
                GompleAst.StructType structType => new StructType(
                    structType
                        .Variables
                        .Select(v => AddType(v.Type))
                        .ToList()
                ),
                GompleAst.TypeRef typeRef => AddType(UnrefType(typeRef)),
                GompleAst.VoidType _ => new EmptyType(),
                GompleAst.FloatType _ => _f64,
                _ => throw new ArgumentException()
            };
            _typeMappings[type] = t;
            return t;
        }

        public GompleAst.Type UnrefType(GompleAst.Type type)
        {
            while (type is GompleAst.TypeRef tr)
                type = _namedTypes[tr.Name.Text];
            return type;
        }
    }

    public class GompleTranslator
    {
        private TypeMapper _typeMapper;

        private TypedGompleAst.Program _program;

        private List<Dictionary<object, (int idx, GompleAst.Type type)>> stack =
            new List<Dictionary<object, (int idx, GompleAst.Type type)>>();

        private void PushLayer()
            => stack.Add(new Dictionary<object, (int idx, GompleAst.Type type)>());

        private List<GompleAst.Type> PopLayer()
        {
            var res = stack[^1].Select(x => x.Value.type).ToList();
            stack.RemoveAt(stack.Count - 1);
            return res;
        }

        private (int idx, int depth) AddName(object name, GompleAst.Type type)
        {
            stack[^1][name] = (stack[^1].Count, type);
            return FindName(name);
        }

        private (int idx, int depth) FindName(object name)
        {
            var (layer, depth) = stack.AsEnumerable().Reverse().Select((v, i) => (v, i))
                .First(d => d.v.ContainsKey(name));
            return (layer[name].idx, depth);
        }

        private Expression DepthGetExpression((int idx, int depth) place)
            => new BinaryExpression(
                new NameExpression(place.depth),
                OperationKind.GetField,
                new ConstExpression(new NumberType(NumberKind.UnsignedInteger, 64), place.idx)
            );

        public Module VisitProgram(TypedGompleAst.Program program)
        {
            PushLayer();
            _typeMapper = new TypeMapper(program);
            _program = program;
            foreach (var func in program.Functions)
            {
                switch (func.Type)
                {
                    case GompleAst.FunctionType _:
                        AddName(func.Name.Text, func.Type);
                        break;
                    case GompleAst.MethodType mth:
                        AddName((func.Name.Text, mth.Sender.Type), func.Type);
                        break;
                }
            }

            var result = new Module(program.Functions.Select(func => (IEntity) VisitFunction(func)).ToList());
            PopLayer();
            return result;
        }

        private Function VisitFunction(TypedGompleAst.Function function)
        {
            PushLayer();

            foreach (var variable in function.Type.Args.Variables)
                AddName(variable.Name.Text, variable.Type);
            if (function.Type is GompleAst.MethodType met)
                AddName(met.Sender.Name.Text, met.Sender.Type);

            var stmts = VisitBlock(function.Body);
            var vars = PopLayer();
            var result = new Function(
                (FunctionType) _typeMapper.AddType(function.Type),
                new Block(
                    new StructType(vars.Select(v => _typeMapper.AddType(v)).ToList()),
                    stmts
                )
            );
            return result;
        }

        private List<Statement> VisitBlock(TypedGompleAst.Block block)
        {
            PushLayer();
            var stmts = block.Statements.Select(VisitStatement).Where(it => it != null).ToList();
            PopLayer();
            return stmts;
        }

        private Statement VisitStatement(TypedGompleAst.Statement statement)
            => statement switch
            {
                TypedGompleAst.AssignStatement assignStatement => new ExpressionStatement(
                    new AssignExpression(
                        VisitExpression(assignStatement.Left),
                        VisitExpression(assignStatement.Right)
                    )
                ),
                TypedGompleAst.ExpressionStatement expressionStatement => new ExpressionStatement(
                    VisitExpression(expressionStatement.Expression)
                ),
                TypedGompleAst.VarAssignStatement varAssignStatement => new ExpressionStatement(
                    new AssignExpression(
                        DepthGetExpression(AddName(varAssignStatement.Name.Text, varAssignStatement.Type)),
                        VisitExpression(varAssignStatement.Value)
                    )
                ),
                TypedGompleAst.ReturnStatement returnStatement => new Return(VisitExpression(returnStatement.Value)),
                TypedGompleAst.ReturnNothingStatement _ => new Return(new ConstExpression(new EmptyType(), null)),
                TypedGompleAst.VarStatement varStatement => VisitVarStatement(varStatement),
                _ => throw new ArgumentOutOfRangeException(nameof(statement))
            };

        private Statement VisitVarStatement(TypedGompleAst.VarStatement varStatement)
        {
            AddName(varStatement.Name.Text, varStatement.Type);
            return null;
        }

        private Expression VisitExpression(TypedGompleAst.Expression expression)
        {
            return expression switch
            {
                TypedGompleAst.BinExpression binExpression => (Expression) new BinaryExpression(
                    VisitExpression(binExpression.Left),
                    VisitOperationKind(binExpression.Op),
                    VisitExpression(binExpression.Right)
                ),
                TypedGompleAst.CallExpression callExpression => VisitCallExpression(callExpression),
                TypedGompleAst.FloatConstExpression floatConstExpression => new ConstExpression(
                    new NumberType(NumberKind.Float, 64),
                    double.Parse(floatConstExpression.Value.Text)
                ),
                TypedGompleAst.GetExpression getExpression => VisitGetExpression(getExpression),
                TypedGompleAst.IntegerConstExpression integerConstExpression => new ConstExpression(
                    new NumberType(NumberKind.SignedInteger, 64),
                    int.Parse(integerConstExpression.Value.Text)
                ),
                TypedGompleAst.NameExpression nameExpression => DepthGetExpression(FindName(nameExpression.Name.Text)),
                _ => throw new ArgumentOutOfRangeException(nameof(expression) + "(" + expression + ")")
            };
        }

        private BinaryExpression VisitGetExpression(TypedGompleAst.GetExpression getExpression)
        {
            var unref = _typeMapper.UnrefType(getExpression.Struct.Type);
            if (unref is GompleAst.StructType str && GetStructFieldOffset(str, getExpression.Field) != null)
                return new BinaryExpression(
                    VisitExpression(getExpression.Struct),
                    OperationKind.GetField,
                    new ConstExpression(
                        new NumberType(NumberKind.UnsignedInteger, 64),
                        GetStructFieldOffset(str, getExpression.Field)
                    )
                );
            throw new ArgumentException();
        }

        private int? GetStructFieldOffset(GompleAst.StructType str, Token field)
            => str
                .Variables
                .Select((v, i) => (v, i))
                .First(v => v.v.Name.Text == field.Text)
                .i;

        private CallExpression VisitCallExpression(TypedGompleAst.CallExpression callExpression)
        {
            if (
                callExpression.Function is TypedGompleAst.GetExpression get
                &&
                _program.Methods.ContainsKey((get.Field.Text, get.Struct.Type))
            )
                return new CallExpression(
                    DepthGetExpression(FindName((get.Field.Text, get.Struct.Type))),
                    new[] {get.Struct}.Concat(callExpression.Params).Select(VisitExpression).ToList()
                );

            return new CallExpression(
                VisitExpression(callExpression.Function),
                callExpression.Params.Select(VisitExpression).ToList()
            );
        }

        private OperationKind VisitOperationKind(Token token)
            => token.Text switch
            {
                "+" => OperationKind.Add,
                "-" => OperationKind.Subtract,
                "*" => OperationKind.Multiply,
                "/" => OperationKind.Divide,
                _ => throw new ArgumentException()
            };
    }
}