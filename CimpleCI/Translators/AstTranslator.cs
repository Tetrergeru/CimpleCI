using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Frontend;
using Frontend.AST;
using Frontend.Lexer;

namespace CimpleCI
{
    public class AstTranslator<T> where T : new()
    {
        public AstTranslator()
        {
        }

        private Dictionary<string, (Type type, Dictionary<string, Action<object, object>> fields)> _data;

        private PrototypeDictionary _pd;

        public T Translate(PrototypeDictionary pd, IASTNode node)
        {
            _pd = pd;
            _data = typeof(T)
                .GetNestedTypes()
                .ToDictionary(
                    m => m.Name,
                    m => (m, m.GetFields().ToDictionary(f => f.Name, f => (Action<object, object>) f.SetValue)));
            var result = new T();
            typeof(T)
                .GetFields()
                .First(f => f.Name == "program")
                .SetValue(result, ParseNode(node));
            return result;
        }

        private object ParseNode(IASTNode node)
        {
            var nodeType = _pd[node.Id()];
            var (type, fields) = _data[nodeType.Name()];
            var result = type
                .GetConstructors()
                .First(c => c.GetParameters().Length == 0)
                .Invoke(new object[] { });

            foreach (var field in fields)
            {
                var nodeField = node[field.Key];
                switch (nodeField)
                {
                    case ASTList lst:
                        field.Value(result, ParseList(lst));
                        break;
                    case ASTObject nd:
                        field.Value(result, ParseNode(nd));
                        break;
                    case ASTLeaf leaf:
                        field.Value(result, leaf.Token);
                        break;
                }
            }

            return result;
        }

        private object ParseList(ASTList list)
        {
            var type = _data[list.Prototype.Type.Name()].type;
            var lstType = typeof(List<>).MakeGenericType(type);
            var lst = lstType.GetConstructors().First(c => c.GetParameters().Length == 0).Invoke(new object[] { });
            var adder = lstType.GetMethods()
                .First(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == type);

            foreach (var elem in list.Values.Select(ParseNode))
                adder.Invoke(lst, new[] {elem});

            return lst;
        }
    }
}