using System.Collections.Generic;

namespace Middleend.Expressions
{
    public class CallExpression : Expression
    {
        public readonly Expression Function;

        public readonly List<Expression> Params;
        
        public CallExpression(Expression function, List<Expression> @params)
        {
            Function = function;
            Params = @params;
        }

        public override T AcceptVisitor<T>(IModuleVisitor<T> visitor)
            => visitor.VisitCallExpression(this);
    }
}