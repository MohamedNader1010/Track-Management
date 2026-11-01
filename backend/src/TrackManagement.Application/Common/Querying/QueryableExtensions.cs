using System.Linq.Expressions;

namespace TrackManagement.Application.Common.Querying;

public static class QueryableExtensions
{
    /// <summary>
    /// Filters on a property mapped through a value converter (smart enums, value objects).
    /// <para>
    /// Writing <c>t.Status == status</c> compiles to a call to the type's overloaded <c>op_Equality</c>, which
    /// EF Core cannot always translate to SQL. A reference-equality node has no operator method attached, so EF
    /// translates it to <c>[Column] = @value</c> and runs the value through the property's converter.
    /// </para>
    /// </summary>
    public static IQueryable<T> WhereEquals<T, TValue>(
        this IQueryable<T> source,
        Expression<Func<T, TValue>> selector,
        TValue value)
        where TValue : class
    {
        var body = Expression.ReferenceEqual(selector.Body, Expression.Constant(value, typeof(TValue)));
        return source.Where(Expression.Lambda<Func<T, bool>>(body, selector.Parameters));
    }
}
