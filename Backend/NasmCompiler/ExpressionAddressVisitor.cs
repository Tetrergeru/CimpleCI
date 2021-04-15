using System;
using System.Collections.Generic;
using Middleend.Expressions;
using Middleend.Types;

namespace Backend.NasmCompiler
{
    public class ExpressionAddressVisitor
    {
        private readonly NasmCompiler _compiler;

        private TypeEvaluator Types => _compiler.Types;

        public ExpressionAddressVisitor(NasmCompiler compiler)
        {
            _compiler = compiler;
        }

        public IEnumerable<Operation> VisitExpression(Expression expression)
        {
            return expression switch
            {
                BinaryExpression binaryExpression => VisitBinaryExpression(binaryExpression),
                MagicExpression magicExpression => VisitMagicExpression(magicExpression),
                NameExpression nameExpression => VisitNameExpression(nameExpression),
                UnaryExpression unaryExpression => VisitUnaryExpression(unaryExpression),
                GetFieldExpression getFieldExpression => VisitGetFieldExpression(getFieldExpression),
                _ => throw new ArgumentOutOfRangeException(nameof(expression) + $"was {expression}"),
            };
        }

        private IEnumerable<Operation> VisitNameExpression(NameExpression nameExpression)
        {
            yield return nameExpression.Depth switch
            {
                0 => throw new Exception("Cannot get address of global scope"),
                1 => Operation.Mov(_compiler.NewRegister(8), new Shift(Register.Rsp, _compiler.ParameterOffset())),
                2 => Operation.Lea(_compiler.NewRegister(8), new Shift(Register.Rsp, _compiler.VariableOffset())),
                _ => throw new Exception($"Unknown scope {nameExpression.Depth}")
            };
        }

        private IEnumerable<Operation> VisitMagicExpression(MagicExpression magicExpression)
            => new[] {Operation.Mov(_compiler.NewRegister(8), new NameOperand(magicExpression.Name))};

        private IEnumerable<Operation> VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            foreach (var op in binaryExpression.Right.AcceptVisitor(_compiler))
                yield return op;
            var right = _compiler.LastRegister;

            foreach (var op in VisitExpression(binaryExpression.Left))
                yield return op;
            var left = _compiler.LastRegister;

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

        private IEnumerable<Operation> VisitGetFieldExpression(GetFieldExpression expression)
        {
            if (expression.Left is NameExpression ne && ne.Depth == 1)
            {
                var (onStack, idx) = _compiler.CurrentFunctionParams.Params[expression.Field];
                yield return Operation.Lea(
                    _compiler.NewRegister(8),
                    new Shift(Register.Rsp, _compiler.ParameterOffset(onStack, idx))
                );
                yield break;
            }

            var str = (StructType) Types.TypeOf(expression.Left);
            foreach (var op in VisitExpression(expression.Left))
                yield return op;
            yield return Operation.Add(
                _compiler.LastRegister,
                TypeEvaluator.GetOffset(str, expression.Field)
            );
        }

        private IEnumerable<Operation> VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            return unaryExpression.Operator switch
            {
                OperationKind.Dereference => unaryExpression.Right.AcceptVisitor(_compiler),
                _ => throw new ArgumentException(
                    $"Unsupported unary operation {unaryExpression.Operator} in calculating address")
            };
        }
    }
}