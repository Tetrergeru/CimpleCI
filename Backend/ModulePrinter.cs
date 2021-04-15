using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Middleend;
using Middleend.Expressions;
using Middleend.Statements;
using Middleend.Types;

namespace Backend
{
    public class ModulePrinter : IModuleVisitor<string>
    {
        private int _depth;

        private string Tab => string.Join("", Enumerable.Range(0, _depth).Select(_ => "   "));

        public string VisitModule(Module module)
            => string.Join("\n\n", module.Entities.Select(e => e.AcceptVisitor(this)));

        public string VisitFunction(Function function)
        {
            _parsedTypes = new Dictionary<BaseType, int>();
            var header = $"{function.Type.AcceptVisitor(this)}";
            _parsedTypes = new Dictionary<BaseType, int>();
            var code = function.Code.AcceptVisitor(this);
            return $"{header} {code}";
        }

        private Dictionary<BaseType, int> _parsedTypes = new Dictionary<BaseType, int>();

        private int _currentTypeId = 0;

        public string VisitEmptyType(EmptyType emptyType)
            => "Unit";

        public string VisitFunctionType(FunctionType functionType)
        {
            var @params = functionType.Params.AcceptVisitor(this);
            return $"fn {@params} -> {functionType.Result.AcceptVisitor(this)}";
        }

        public string VisitNumberType(NumberType numberType)
        {
            var kind = numberType.NumberKind switch
            {
                NumberKind.UnsignedInteger => "u",
                NumberKind.Float => "f",
                NumberKind.SignedInteger => "i",
                _ => throw new ArgumentOutOfRangeException()
            };
            return $"{kind}{numberType.BitSize}";
        }

        public string VisitPointerType(PointerType pointerType)
            => $"*{pointerType.To.AcceptVisitor(this)}";

        public string VisitStructType(StructType structType)
        {
            if (_parsedTypes.ContainsKey(structType))
            {
                var id = _parsedTypes[structType];
                if (id == -1)
                {
                    id = _currentTypeId++;
                    _parsedTypes[structType] = id;
                }

                return $"@Id({id})";
            }

            _parsedTypes[structType] = -1;
            var result = $"{{{string.Join(",", structType.Fields.Select(f => f.AcceptVisitor(this)))}}}";
            ;
            if (_parsedTypes[structType] != -1)
                result = $"Id({_parsedTypes[structType]}) = {result}";
            return result;
        }

        public string VisitArrayType(ArrayType arrayType)
            => $"[{arrayType.ElemType.AcceptVisitor(this)};{arrayType.Length}]";

        public string VisitBlock(Block block)
        {
            _depth++;
            var decl = Tab + "local " + block.Variables.AcceptVisitor(this);
            var code = string.Join("\n", block.Statements.Select(s => s.AcceptVisitor(this)));
            _depth--;
            return $"{{\n{decl}\n{code}\n{Tab}}}";
        }

        public string VisitConditional(Conditional conditional)
            => $"{Tab}if {conditional.Condition.AcceptVisitor(this)} " +
               $"{conditional.Then.AcceptVisitor(this)} " +
               $"else {conditional.Else.AcceptVisitor(this)}";

        public string VisitCycle(Cycle cycle)
            => $"{Tab}while {cycle.Condition.AcceptVisitor(this)} " +
               $"{cycle.Body.AcceptVisitor(this)}";

        public string VisitExpressionStatement(ExpressionStatement expressionStatement)
            => $"{Tab}{expressionStatement.Expr.AcceptVisitor(this)};";

        public string VisitReturn(Return @return)
            => $"{Tab}return {@return.Value?.AcceptVisitor(this)};";

        public string VisitMagicExpression(MagicExpression magicExpression)
            => $"{magicExpression.Name}";

        public string VisitBinaryExpression(BinaryExpression binaryExpression)
            => $"({binaryExpression.Left.AcceptVisitor(this)} " +
               $"{binaryExpression.Operator.ToSymbol()} " +
               $"{binaryExpression.Right.AcceptVisitor(this)})";

        public string VisitUnaryExpression(UnaryExpression unaryExpression)
            => $"({unaryExpression.Operator.ToSymbol()}" +
               $"{unaryExpression.Right.AcceptVisitor(this)})";

        public string VisitAssignExpression(AssignExpression assignExpression)
            => $"{assignExpression.Left.AcceptVisitor(this)} " +
               $"{assignExpression.PreAssignOperator?.ToSymbol() ?? ""}= " +
               $"{assignExpression.Right.AcceptVisitor(this)}";

        public string VisitCallExpression(CallExpression callExpression)
            => $"{callExpression.Function.AcceptVisitor(this)}" +
               $"({string.Join(", ", callExpression.Params.Select(p => p.AcceptVisitor(this)))})";

        public string VisitConstExpression(ConstExpression constExpression)
            => constExpression.Type is EmptyType ? "unit" : constExpression.Value.ToString();

        public string VisitNameExpression(NameExpression nameExpression)
            => $"<{nameExpression.Depth}>";

        public string VisitGetFieldExpression(GetFieldExpression expression)
            => $"({expression.Left.AcceptVisitor(this)}.{expression.Field})";
    }
}