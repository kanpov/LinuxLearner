using System.Linq.Expressions;

namespace LinuxLearner.Utilities;

public static class QueryableExtensions
{
    public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> queryable, bool condition, Expression<Func<TSource, bool>> predicate)
    {
        return condition ? queryable.Where(predicate) : queryable;
    }

    public static IOrderedQueryable<TSource> OrderWithReversal<TSource, TKey>(this IQueryable<TSource> queryable,
        bool reverse, Expression<Func<TSource, TKey>> keySelector)
    {
        return reverse ? queryable.OrderByDescending(keySelector) : queryable.OrderBy(keySelector);
    }
}