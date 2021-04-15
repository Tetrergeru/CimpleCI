using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.AST;
using Middleend;
using Middleend.Expressions;
using Middleend.Statements;
using Middleend.Types;

namespace CimpleCI
{
    public class Cimple0Translator
    {
        private static BaseType u64 = new NumberType(NumberKind.UnsignedInteger, 64);

        private List<Dictionary<string, int>> indices = new List<Dictionary<string, int>>
            {new Dictionary<string, int>()};

        private void AddName(string name, int idx) =>
            indices[^1][name] = idx;

        private (int idx, int depth) FindName(string name)
        {
            var (dictionary, i) = indices.AsEnumerable().Select((v, i) => (v, i)).Reverse()
                .First(d => d.v.ContainsKey(name));
            return (dictionary[name], i);
        }

        private string VisitName(IASTNode node)
            => ((ASTLeaf) node).Text;

        private BaseType VisitType(IASTNode node)
            => new NumberType(NumberKind.UnsignedInteger, int.Parse(VisitName(node)[1..]));

        private FunctionType FunctionType(IASTNode @params, BaseType returnType)
        {
            foreach (var (name, i) in @params
                .Enumerate()
                .Select((param, i) => (name: VisitName(param["Name"]), i)))
                AddName(name, i);
            return new FunctionType(
                new StructType(@params
                    .Enumerate()
                    .Select(param => VisitType(param["Type"]))
                    .ToList()),
                returnType);
        }

        private static HashSet<string> Assigns = new HashSet<string> {"=", "+=", "-=", "*=", "/="};

        private Expression VisitBinExpr(IASTNode expr)
        {
            var op = VisitName(expr["Op"]);
            var (left, right) = (VisitExpression(expr["Left"]), VisitExpression(expr["Right"]));
            if (Assigns.Contains(op))
                return new AssignExpression(left, right,
                    op != "=" ? (OperationKind?) OperationKindClass.ParseOp(op[..^1], true) : null);

            return new BinaryExpression(left, OperationKindClass.ParseOp(op, true), right);
        }

        private Expression VisitExpression(IASTNode expr)
        {
            return ((ASTObject) expr).Prototype.Name() switch
            {
                "BinExpression" => VisitBinExpr(expr),
                "UnExpression" => new UnaryExpression(OperationKindClass.ParseOp(VisitName(expr["Op"]), false),
                    VisitExpression(expr["Right"])),
                "CallExpression" => VisitCallExpression(expr),
                "ConstExpression" => new ConstExpression(u64, VisitName(expr["Value"])),
                "NameExpression" => VisitNameExpression(expr),
                "ParExpression" => VisitExpression(expr["Expr"]),
                _ => throw new ArgumentException()
            };
        }

        private Expression VisitNameExpression(IASTNode expr)
        {
            var (idx, depth) = FindName(VisitName(expr["Name"]));
            return new GetFieldExpression(new NameExpression(depth), idx);
        }

        private CallExpression VisitCallExpression(IASTNode expr)
        {
            var (idx, depth) = FindName(VisitName(expr["Function"]));
            return new CallExpression(
                new GetFieldExpression(new NameExpression(depth), idx),
                expr["Params"].Enumerate().Select(VisitExpression).ToList());
        }

        private Conditional VisitConditionOperator(IASTNode @operator)
            => new Conditional(
                VisitExpression(@operator["Condition"]),
                VisitBlock(@operator["If"]),
                VisitBlock(@operator["Else"]));

        private Statement VisitOperator(IASTNode @operator)
        {
            return ((ASTObject) @operator).Prototype.Name() switch
            {
                "ConditionOperator" => VisitConditionOperator(@operator),
                "ReturnOperator" => new Return(VisitExpression(@operator["Value"])),
                "ExpressionOperator" => new ExpressionStatement(VisitExpression(@operator["Expression"])),
                "WhileOperator" => new Cycle(VisitExpression(@operator["Condition"]), VisitBlock(@operator["Body"])),
                _ => throw new ArgumentException(),
            };
        }

        private Block VisitBlock(IASTNode block)
        {
            indices.Add(new Dictionary<string, int>());
            var statements = new List<Statement>();
            var variables = new List<BaseType>();
            foreach (var astNode in block["Operators"].Enumerate())
            {
                var op = (ASTObject) astNode;
                switch (op.Prototype.Name())
                {
                    case "DeclarationOperator":
                        AddName(VisitName(op["Name"]), variables.Count);
                        variables.Add(VisitType(op["Type"]));
                        break;
                    case "ArrayDeclarationOperator":
                        AddName(VisitName(op["Name"]), variables.Count);
                        variables.Add(
                            new ArrayType(VisitType(op["Type"]),
                                int.Parse(VisitName(op["Size"]))));
                        break;
                    default:
                        statements.Add(VisitOperator(op));
                        break;
                }
            }

            indices.RemoveAt(indices.Count - 1);
            return new Block(new StructType(variables), statements);
        }

        private Function VisitFunction(IASTNode node)
        {
            indices.Add(new Dictionary<string, int>());
            var type = FunctionType(node["Params"], VisitType(node["Type"]));
            var result = new Function(type, VisitBlock(node["Block"]));
            indices.RemoveAt(indices.Count - 1);
            return result;
        }

        private Module VisitProgram(IASTNode program)
            => new Module(program["Functions"].Enumerate().Select(f => (IEntity) VisitFunction(f)).ToList());

        public static Module Parse(IASTNode node)
            => new Cimple0Translator().VisitProgram(node);
    }
}