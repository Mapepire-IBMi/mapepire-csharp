namespace Mapepire.EntityFrameworkCore.Query.Internal;

/// <summary>Generates Db2 for i SQL from EF Core query expression trees.</summary>
public class MapepireQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies) : QuerySqlGenerator(dependencies)
{
    /// <summary>
    /// Db2 for i rejects untyped parameter markers in many positions (select list, both sides of a
    /// comparison, CASE results, IS NULL), so parameters are emitted as <c>CAST(? AS type)</c>.
    /// </summary>
    protected override Expression VisitSqlParameter(SqlParameterExpression sqlParameterExpression)
    {
        var storeType = sqlParameterExpression.TypeMapping?.StoreType;
        if (storeType == null)
        {
            return base.VisitSqlParameter(sqlParameterExpression);
        }

        Sql.Append("CAST(");
        base.VisitSqlParameter(sqlParameterExpression);
        Sql.Append(" AS ").Append(storeType).Append(")");
        return sqlParameterExpression;
    }

    /// <summary>A SELECT without a FROM clause is not valid in Db2 for i.</summary>
    protected override void GeneratePseudoFromClause() => Sql.Append(" FROM SYSIBM.SYSDUMMY1");

    /// <inheritdoc />
    protected override Expression VisitSqlBinary(SqlBinaryExpression sqlBinaryExpression)
    {
        var function = sqlBinaryExpression.OperatorType switch
        {
            ExpressionType.Modulo => "MOD",
            ExpressionType.And when sqlBinaryExpression.Type.UnwrapNullable() != typeof(bool) => "BITAND",
            ExpressionType.Or when sqlBinaryExpression.Type.UnwrapNullable() != typeof(bool) => "BITOR",
            ExpressionType.ExclusiveOr => "BITXOR",
            _ => null,
        };

        if (function == null)
        {
            return base.VisitSqlBinary(sqlBinaryExpression);
        }

        Sql.Append(function).Append("(");
        Visit(sqlBinaryExpression.Left);
        Sql.Append(", ");
        Visit(sqlBinaryExpression.Right);
        Sql.Append(")");
        return sqlBinaryExpression;
    }

    /// <inheritdoc />
    protected override string GetOperator(SqlBinaryExpression binaryExpression)
        => binaryExpression.OperatorType switch
        {
            ExpressionType.Add when binaryExpression.Type == typeof(string) => " || ",
            // Non-short-circuit boolean operators on 0/1 values (see search condition conversion).
            ExpressionType.And => " AND ",
            ExpressionType.Or => " OR ",
            _ => base.GetOperator(binaryExpression),
        };
}

/// <summary>Creates <see cref="MapepireQuerySqlGenerator"/> instances.</summary>
public class MapepireQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies) : IQuerySqlGeneratorFactory
{
    /// <inheritdoc />
    public virtual QuerySqlGenerator Create() => new MapepireQuerySqlGenerator(dependencies);
}

internal static class QueryTypeExtensions
{
    public static Type UnwrapNullable(this Type type) => Nullable.GetUnderlyingType(type) ?? type;
}
