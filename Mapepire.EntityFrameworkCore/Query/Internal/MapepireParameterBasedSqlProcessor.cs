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
    public override Expression Process(Expression queryExpression, ParametersCacheDecorator parametersDecorator)
    {
        var optimized = base.Process(queryExpression, parametersDecorator);
        optimized = new LimitOffsetInliningExpressionVisitor(Dependencies.SqlExpressionFactory, parametersDecorator)
            .Visit(optimized);

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
    ParametersCacheDecorator parametersDecorator) : ExpressionVisitor
{
    /// <summary>Inlines the OFFSET and FETCH values of a SELECT, after rewriting its nested SELECTs.</summary>
    protected override Expression VisitExtension(Expression node)
    {
        var visited = base.VisitExtension(node);
        if (visited is not SelectExpression select || (select.Offset is null && select.Limit is null))
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

    /// <summary>Replaces an integer parameter with a literal of its current value.</summary>
    /// <param name="expression">The OFFSET or FETCH expression, if any.</param>
    /// <returns>The literal, or the expression unchanged if it is not an integer parameter.</returns>
    private SqlExpression? Inline(SqlExpression? expression)
    {
        if (expression is SqlParameterExpression parameter
            && parametersDecorator.GetAndDisableCaching().TryGetValue(parameter.Name, out var value)
            && value is int or long)
        {
            return sqlExpressionFactory.Constant(Convert.ToInt32(value), parameter.TypeMapping);
        }

        return expression;
    }
}
