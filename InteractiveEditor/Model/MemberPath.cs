using System.Linq.Expressions;

namespace InteractiveEditor.Model;

// Turns a selector (f => f.Moo.MooX) into the path the indexer takes ("Moo.MooX").
internal static class MemberPath
{
    public static string Of(LambdaExpression selector)
    {
        var names = new Stack<string>();
        var body = selector.Body;

        // A member of a value type comes boxed to object.
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } boxed)
            body = boxed.Operand;

        while (body is MemberExpression member)
        {
            names.Push(member.Member.Name);
            body = member.Expression;
        }

        if (body != selector.Parameters[0] || names.Count == 0)
        {
            throw new ArgumentException(
                $"A selector is a chain of members of its parameter, as in f => f.Moo.MooX; '{selector}' is not.", nameof(selector));
        }

        return string.Join('.', names);
    }
}
