﻿using System.Linq.Expressions;

namespace AppEngine.DataAccess;

public static class QueryableExtensions
{
    public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> source,
                                                       bool condition,
                                                       Expression<Func<TSource, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static IQueryable<TSource> WhereNotNull<TSource>(this IQueryable<TSource?> source)
        where TSource : struct
    {
        return source.Where(x => x != null)
                     .Select(x => x!.Value);
    }
}