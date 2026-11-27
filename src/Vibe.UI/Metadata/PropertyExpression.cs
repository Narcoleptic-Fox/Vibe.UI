using System.Linq.Expressions;
using System.Reflection;

namespace Vibe.UI.Metadata;

/// <summary>Reads the property out of an accessor expression. The only place expressions are taken apart.</summary>
internal static class PropertyExpression
{
    /// <summary>Returns the member access in <paramref name="expression"/>, looking through a boxing or widening conversion.</summary>
    public static MemberExpression GetPropertyAccess(LambdaExpression expression) =>
        FindPropertyAccess(expression)
        ?? throw new ArgumentException(
            $"Expected a property access such as x => x.Name or () => model.Name, but got '{expression.Body}'.",
            nameof(expression)
        );

    /// <summary>The same as <see cref="GetPropertyAccess"/>, but null when the expression reads something that is not a property, such as a field.</summary>
    public static MemberExpression? FindPropertyAccess(LambdaExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        Expression body = expression.Body;
        while (
            body
                is UnaryExpression
                {
                    NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
                } convert
        )
            body = convert.Operand;

        return body is MemberExpression { Member: PropertyInfo } member ? member : null;
    }

    /// <summary>Returns the property in an expression of the form <c>x =&gt; x.Name</c>, rejecting nested access.</summary>
    public static PropertyInfo GetDirectProperty(LambdaExpression expression)
    {
        MemberExpression member = GetPropertyAccess(expression);
        if (member.Expression is not ParameterExpression)
            throw new ArgumentException(
                $"Expected a property directly on the parameter, such as x => x.Name, but got '{expression.Body}'.",
                nameof(expression)
            );
        return (PropertyInfo)member.Member;
    }
}
