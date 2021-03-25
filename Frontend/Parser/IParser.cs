using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using Frontend.AST;
using Frontend.Lexer;

namespace Frontend.Parser
{
    public interface IParser
    {
        IASTNode Parse(List<Token> code);
    }
}