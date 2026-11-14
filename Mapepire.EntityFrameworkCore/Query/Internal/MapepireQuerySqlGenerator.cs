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
        if (storeType is null)
        {
            return base.VisitSqlParameter(sqlParameterExpression);
        }

        Sql.Append("CAST(");
        base.VisitSqlParameter(sqlParameterExpression);
        Sql.Append(" AS ").Append(storeType).Append(")");
        return sqlParameterExpression;
    }

    /// <summary>
    /// Db2 for i rejects subquery predicates (<c>EXISTS</c>, <c>IN (subquery)</c>) inside a CASE WHEN condition
    /// (SQL0104 / SQL0115), but accepts them in a WHERE clause and accepts scalar subqueries in CASE. Such
    /// conditions are therefore written as <c>WHEN (SELECT 1 FROM SYSIBM.SYSDUMMY1 WHERE condition) = 1</c>;
    /// the subquery is NULL when the condition is not true, so the ELSE branch is taken as before.
    /// </summary>
    protected override Expression VisitCase(CaseExpression caseExpression)
    {
        Sql.Append("CASE");

        if (caseExpression.Operand is not null)
        {
            Sql.Append(" ");
            Visit(caseExpression.Operand);
        }

        using (Sql.Indent())
        {
            foreach (var whenClause in caseExpression.WhenClauses)
            {
                Sql.AppendLine().Append("WHEN ");
                if (caseExpression.Operand is null && SubqueryPredicateFinder.HasSubqueryPredicate(whenClause.Test))
                {
                    Sql.Append("(SELECT 1 FROM SYSIBM.SYSDUMMY1 WHERE ");
                    Visit(whenClause.Test);
                    Sql.Append(") = 1");
                }
                else
                {
                    Visit(whenClause.Test);
                }

                Sql.Append(" THEN ");
                Visit(whenClause.Result);
            }

            if (caseExpression.ElseResult is not null)
            {
                Sql.AppendLine().Append("ELSE ");
                Visit(caseExpression.ElseResult);
            }
        }

        Sql.AppendLine().Append("END");
        return caseExpression;
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

        if (function is null)
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

    /// <summary>Finds EXISTS / IN (subquery) predicates, without looking inside nested SELECTs.</summary>
    private sealed class SubqueryPredicateFinder : ExpressionVisitor
    {
        private bool _found;

        /// <summary>Whether a condition contains EXISTS or IN (subquery) outside any nested SELECT.</summary>
        public static bool HasSubqueryPredicate(Expression expression)
        {
            var finder = new SubqueryPredicateFinder();
            finder.Visit(expression);
            return finder._found;
        }

        /// <summary>Stops at the first subquery predicate, and does not descend into nested SELECTs.</summary>
        public override Expression? Visit(Expression? node)
        {
            if (_found || node is null)
            {
                return node;
            }

            switch (node)
            {
                case ExistsExpression:
                case InExpression { Subquery: not null }:
                    _found = true;
                    return node;
                case SelectExpression:
                    return node;
                default:
                    return base.Visit(node);
            }
        }
    }
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
