namespace Mapepire.EntityFrameworkCore.Query.Internal;

/// <summary>
/// Final, parameter-aware rewrites: OFFSET/FETCH values are inlined as literals and boolean
/// values/conditions are converted to the form Db2 for i accepts.
/// </summary>
public class MapepireParameterBasedSqlProcessor(
    RelationalParameterBasedSqlProcessorDependencies dependencies,
    RelationalParameterBasedSqlProcessorParameters parameters)
    : RelationalParameterBasedSqlProcessor(dependencies, parameters)
{
    /// <inheritdoc />
    public override Expression Optimize(
        Expression queryExpression,
        IReadOnlyDictionary<string, object?> parametersValues,
        out bool canCache)
    {
        var optimized = base.Optimize(queryExpression, parametersValues, out canCache);

        optimized = new LimitOffsetInliningExpressionVisitor(Dependencies.SqlExpressionFactory, parametersValues)
            .Process(optimized, out var canCacheLimits);
        canCache &= canCacheLimits;

        return new MapepireSearchConditionConvertingExpressionVisitor(Dependencies.SqlExpressionFactory).Visit(optimized);
    }
}

/// <summary>Creates <see cref="MapepireParameterBasedSqlProcessor"/> instances.</summary>
public class MapepireParameterBasedSqlProcessorFactory(RelationalParameterBasedSqlProcessorDependencies dependencies)
    : IRelationalParameterBasedSqlProcessorFactory
{
    /// <inheritdoc />
    public virtual RelationalParameterBasedSqlProcessor Create(RelationalParameterBasedSqlProcessorParameters parameters)
        => new MapepireParameterBasedSqlProcessor(dependencies, parameters);
}

/// <summary>
/// Replaces parameter markers in OFFSET / FETCH FIRST with integer literals, and turns
/// <c>Take(0)</c> into a false predicate (Db2 requires a positive FETCH FIRST row count).
/// </summary>
internal sealed class LimitOffsetInliningExpressionVisitor(
    ISqlExpressionFactory sqlExpressionFactory,
    IReadOnlyDictionary<string, object?> parameterValues) : ExpressionVisitor
{
    private bool _canCache = true;

    public Expression Process(Expression expression, out bool canCache)
    {
        var result = Visit(expression);
        canCache = _canCache;
        return result;
    }

    protected override Expression VisitExtension(Expression node)
    {
        var visited = base.VisitExtension(node);
        if (visited is not SelectExpression select || (select.Offset == null && select.Limit == null))
        {
            return visited;
        }

        var offset = Inline(select.Offset);
        var limit = Inline(select.Limit);

        if (limit is SqlConstantExpression { Value: 0 })
        {
            return select.Update(
                select.Tables,
                select.GroupBy.Count > 0 ? select.Predicate : sqlExpressionFactory.Constant(false),
                select.GroupBy,
                select.GroupBy.Count > 0 ? sqlExpressionFactory.Constant(false) : null,
                select.Projection,
                new List<OrderingExpression>(0),
                offset: null,
                limit: null);
        }

        if (offset is SqlConstantExpression { Value: 0 })
        {
            offset = null;
        }

        return ReferenceEquals(offset, select.Offset) && ReferenceEquals(limit, select.Limit)
            ? select
            : select.Update(
                select.Tables,
                select.Predicate,
                select.GroupBy,
                select.Having,
                select.Projection,
                select.Orderings,
                offset,
                limit);
    }

    private SqlExpression? Inline(SqlExpression? expression)
    {
        if (expression is SqlParameterExpression parameter
            && parameterValues.TryGetValue(parameter.Name, out var value)
            && value is int or long)
        {
            _canCache = false;
            return sqlExpressionFactory.Constant(Convert.ToInt32(value), parameter.TypeMapping);
        }

        return expression;
    }
}
