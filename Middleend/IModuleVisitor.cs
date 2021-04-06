using Middleend.Expressions;
using Middleend.Statements;
using Middleend.Types;

namespace Middleend
{
    public interface IModuleVisitor<out T>
    {
        T VisitModule(Module module);

        // Visit Entities
        
        T VisitFunction(Function function);
        
        // Visit Types

        T VisitEmptyType(EmptyType emptyType);

        T VisitFunctionType(FunctionType functionType);

        T VisitNumberType(NumberType numberType);

        T VisitPointerType(PointerType pointerType);

        T VisitStructType(StructType structType);

        T VisitArrayType(ArrayType arrayType);
        
        // Visit Statements
        
        T VisitBlock(Block block);
        
        T VisitConditional(Conditional conditional);
        
        T VisitCycle(Cycle cycle);
        
        T VisitExpressionStatement(ExpressionStatement expressionStatement);
        
        T VisitReturn(Return @return);

        // Visit Expressions

        T VisitMagicExpression(MagicExpression magicExpression);
        
        T VisitBinaryExpression(BinaryExpression binaryExpression);

        T VisitUnaryExpression(UnaryExpression unaryExpression);

        T VisitAssignExpression(AssignExpression assignExpression);

        T VisitCallExpression(CallExpression callExpression);

        T VisitConstExpression(ConstExpression constExpression);

        T VisitNameExpression(NameExpression nameExpression);

        T VisitParExpression(ParExpression parExpression);
    }
}