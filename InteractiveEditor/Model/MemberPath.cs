using System.Linq.Expressions;
using System.Reflection;

namespace InteractiveEditor.Model;

// Turns a selector (f => f.Moo.MooX) into the chain of members it goes through (Moo, then MooX).
internal static class MemberPath
{
    public static List<MemberInfo> Of(LambdaExpression selector)
    {
        var members = new List<MemberInfo>();
        var body = selector.Body;

        // A member of a value type comes boxed to object.
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } boxed)
            body = boxed.Operand;

        while (body is MemberExpression member)
        {
            members.Insert(0, member.Member);
            body = member.Expression;
        }

        if (body != selector.Parameters[0] || members.Count == 0)
        {
            throw new ArgumentException(
                $"A selector is a chain of members of its parameter, as in f => f.Moo.MooX; '{selector}' is not.", nameof(selector));
        }

        return members;
    }
}
