using System;
using System.Collections.Generic;
using System.Linq;
using Middleend;
using Middleend.Expressions;
using Middleend.Types;

namespace Backend.NasmCompiler
{
    public class TypeEvaluator
    {
        private readonly NasmCompiler _compiler;
        
        private readonly Dictionary<object, BaseType> _typeCache = new Dictionary<object, BaseType>();

        public TypeEvaluator(NasmCompiler compiler)
        {
            _compiler = compiler;
        }

        public BaseType TypeOf(Expression expression)
        {
            if (_typeCache.ContainsKey(expression))
                return _typeCache[expression];
            BaseType result;
            switch (expression)
            {
                case AssignExpression _:
                    result = new EmptyType();
                    break;
                case BinaryExpression binaryExpression:
                {
                    var l = TypeOf(binaryExpression.Left);
                    var r = TypeOf(binaryExpression.Right);
                    result = binaryExpression.Operator switch
                    {
                        OperationKind.Add => VisitNumericOperation(l, r,
                            $"{binaryExpression.Left} and {binaryExpression.Right} are not valid addition operands"),
                        OperationKind.Multiply => VisitNumericOperation(l, r,
                            $"{binaryExpression.Left} and {binaryExpression.Right} are not valid multiplication operands"),
                    };
                    break;
                }
                case CallExpression callExpression:
                {
                    var func = TypeOf(callExpression.Function);
                    // TODO
                    // var args = callExpression.Params.Select(TypeOf).ToList();
                    if (!(func is FunctionType f))
                        throw new Exception("Function must be callable");
                    result = f.Result;
                    break;
                }
                case ConstExpression constExpression:
                    result = constExpression.Type;
                    break;
                case MagicExpression _:
                    result = new FunctionType(new StructType(new NumberType(NumberKind.SignedInteger, 64)), new EmptyType());
                    break;
                case NameExpression nameExpression:
                    result = nameExpression.Depth switch
                    {
                        1 => (_compiler.Module.Entities[_compiler.CurrentFunction] as Function)?.Type.Params,
                        2 => (_compiler.Module.Entities[_compiler.CurrentFunction] as Function)?.Code.Variables,
                        _ => throw new ArgumentException(),
                    };
                    break;
                case UnaryExpression unaryExpression:
                {
                    var r = TypeOf(unaryExpression.Right);
                    result = unaryExpression.Operator switch
                    {
                        OperationKind.Dereference => r is PointerType pt
                            ? pt.To
                            : throw new Exception("Invalid operand for dereferencing"),
                        _ => throw new ArgumentException(),
                    };
                    break;
                }
                case GetFieldExpression getFieldExpression:
                {
                    var field = getFieldExpression.Field;
                    var left = getFieldExpression.Left;
                    if (left is NameExpression ne && ne.Depth == 0)
                        return (_compiler.Module.Entities[field] as Function)?.Type;
                    if (TypeOf(left) is StructType st)
                        return st.Fields[field];
                    throw new Exception($"Cannot get field {field} of struct {left}");
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }

            _typeCache[expression] = result;
            return result;
        }

        private BaseType VisitNumericOperation(BaseType l, BaseType r, string text)
        {
            return l is NumberType lN && r is NumberType rN &&
                   lN.NumberKind == rN.NumberKind && lN.BitSize == rN.BitSize
                ? l
                : throw new Exception(text);
        }
        
        public static int GetOffset(StructType str, int i)
        {
            var varSizes = str.Fields.Select(SizeOf).ToList();
            var lastChunk = 0;
            var offset = 0;
            foreach (var size in varSizes.Take(i))
            {
                if (size <= lastChunk)
                    offset += size;
                else
                    offset = RoundBy8(offset) + size;
                lastChunk = size;
            }

            return offset;
        }

        public static int SizeOf(BaseType type)
        {
            return type switch
            {
                ArrayType arrayType => SizeOf(arrayType.ElemType) * arrayType.Length,
                EmptyType _ => 0,
                FunctionType _ => 0,
                NumberType numberType => numberType.BitSize / 8,
                PointerType _ => 8,
                StructType structType => SizeOfStruct(structType),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static int SizeOfStruct(StructType str)
        {
            var varSizes = str.Fields.Select(SizeOf).ToList();
            var lastChunk = 0;
            var offset = 0;
            foreach (var size in varSizes)
            {
                if (size <= lastChunk)
                    offset += size;
                else
                    offset = RoundBy8(offset) + size;
                lastChunk = size;
            }

            return offset;
        }

        public static int RoundBy8(int val)
        {
            if (val % 8 == 0)
                return val;
            return val / 8 + 1;
        }
    }
}