using System;
using Middleend.Expressions;
using Middleend.Statements;
using Middleend.Types;

namespace Middleend
{
    public interface IModuleVisitor<out T>
    {
        T VisitModule(Module module) => throw new NotImplementedException();

        // Visit Entities
        T VisitFunction(Function function) => throw new NotImplementedException();

        // Visit Types
        T VisitEmptyType(EmptyType emptyType) => throw new NotImplementedException();
        T VisitFunctionType(FunctionType functionType) => throw new NotImplementedException();
        T VisitNumberType(NumberType numberType) => throw new NotImplementedException();
        T VisitPointerType(PointerType pointerType) => throw new NotImplementedException();
        T VisitStructType(StructType structType) => throw new NotImplementedException();
        T VisitArrayType(ArrayType arrayType) => throw new NotImplementedException();

        // Visit Statements
        T VisitBlock(Block block) => throw new NotImplementedException();
        T VisitConditional(Conditional conditional) => throw new NotImplementedException();
        T VisitCycle(Cycle cycle) => throw new NotImplementedException();
        T VisitExpressionStatement(ExpressionStatement expressionStatement) => throw new NotImplementedException();
        T VisitReturn(Return @return) => throw new NotImplementedException();

        // Visit Expressions
        // T VisitExpression(Expression expression) => expression switch
        // {
        //     AssignExpression assignExpression => VisitAssignExpression(assignExpression),
        //     BinaryExpression binaryExpression => VisitBinaryExpression(binaryExpression),
        //     CallExpression callExpression => VisitCallExpression(callExpression),
        //     ConstExpression constExpression => VisitConstExpression(constExpression),
        //     GetFieldExpression getFieldExpression => VisitGetFieldExpression(getFieldExpression),
        //     MagicExpression magicExpression => VisitMagicExpression(magicExpression),
        //     NameExpression nameExpression => VisitNameExpression(nameExpression),
        //     UnaryExpression unaryExpression => VisitUnaryExpression(unaryExpression),
        //     _ => throw new ArgumentOutOfRangeException(nameof(expression))
        // };

        T VisitMagicExpression(MagicExpression magicExpression) => throw new NotImplementedException();
        T VisitBinaryExpression(BinaryExpression binaryExpression) => throw new NotImplementedException();
        T VisitUnaryExpression(UnaryExpression unaryExpression) => throw new NotImplementedException();
        T VisitAssignExpression(AssignExpression assignExpression) => throw new NotImplementedException();
        T VisitCallExpression(CallExpression callExpression) => throw new NotImplementedException();
        T VisitConstExpression(ConstExpression constExpression) => throw new NotImplementedException();
        T VisitNameExpression(NameExpression nameExpression) => throw new NotImplementedException();
        T VisitGetFieldExpression(GetFieldExpression expression) => throw new NotImplementedException();
    }
}