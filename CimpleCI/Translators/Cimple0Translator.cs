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
        private string VisitName(IASTNode node)
            => ((ASTLeaf) node).Text;

        private BaseType VisitType(IASTNode node)
            => new NumberType(NumberKind.UnsignedInteger, int.Parse(VisitName(node)[1..]));

        private FunctionType FunctionType(IASTNode @params, BaseType returnType)
            => new FunctionType(
                @params
                    .Enumerate()
                    .Select(param => (VisitName(param["Name"]), VisitType(param["Type"])))
                    .ToList(),
                returnType);

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
                "CallExpression" => new CallExpression(new NameExpression(VisitName(expr["Function"])),
                    expr["Params"].Enumerate().Select(VisitExpression).ToList()),
                "ConstExpression" => new ConstExpression(u64, VisitName(expr["Value"])),
                "NameExpression" => new NameExpression(VisitName(expr["Name"])),
                _ => throw new ArgumentException()
            };
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
            var statements = new List<Statement>();
            var variables = new List<(string name, BaseType type)>();
            foreach (var astNode in block["Operators"].Enumerate())
            {
                var op = (ASTObject) astNode;
                switch (op.Prototype.Name())
                {
                    case "DeclarationOperator":
                        variables.Add((VisitName(op["Name"]), VisitType(op["Type"])));
                        break;
                    case "ArrayDeclarationOperator":
                        variables.Add((VisitName(op["Name"]),
                            new ArrayType(VisitType(op["Type"]),
                                int.Parse(VisitName(op["Size"])))));
                        break;
                    default:
                        statements.Add(VisitOperator(op));
                        break;
                }
            }
            return new Block(variables, statements);
        }

        private Function VisitFunction(IASTNode node)
            => new Function(VisitName(node["Name"]), FunctionType(node["Params"], VisitType(node["Type"])),
                VisitBlock(node["Block"]));

        private Module VisitProgram(IASTNode program)
            => new Module(program["Functions"].Enumerate().Select(f => (IEntity)VisitFunction(f)).ToList());

        public static Module Parse(IASTNode node)
            => new Cimple0Translator().VisitProgram(node);
    }
}