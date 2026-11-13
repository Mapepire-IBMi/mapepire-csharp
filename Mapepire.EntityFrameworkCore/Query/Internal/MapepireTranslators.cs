namespace Mapepire.EntityFrameworkCore.Query.Internal;

/// <summary>Registers the Db2 for i method call translators.</summary>
public class MapepireMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
{
    /// <summary>Creates the provider and registers the Db2 for i translators after the relational ones.</summary>
    public MapepireMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        var sqlExpressionFactory = dependencies.SqlExpressionFactory;
        AddTranslators(
        [
            new MapepireStringMethodTranslator(sqlExpressionFactory),
            new MapepireMathTranslator(sqlExpressionFactory),
        ]);
    }
}

/// <summary>Registers the Db2 for i member translators.</summary>
public class MapepireMemberTranslatorProvider : RelationalMemberTranslatorProvider
{
    /// <summary>Creates the provider and registers the Db2 for i translators after the relational ones.</summary>
    public MapepireMemberTranslatorProvider(RelationalMemberTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        var sqlExpressionFactory = dependencies.SqlExpressionFactory;
        AddTranslators(
        [
            new MapepireStringMemberTranslator(sqlExpressionFactory),
            new MapepireDateTimeMemberTranslator(sqlExpressionFactory),
        ]);
    }
}

/// <summary>Translates <see cref="string"/> instance and static methods.</summary>
public class MapepireStringMethodTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMethodCallTranslator
{
    private const char LikeEscapeChar = '\\';

    // ApplyTypeMapping returns null only for a null input, and the inputs below are never null, so "!" is safe.

    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(string))
        {
            return null;
        }

        if (instance is null)
        {
            if (method.Name == nameof(string.IsNullOrWhiteSpace) && arguments.Count == 1)
            {
                var argument = arguments[0];
                return sqlExpressionFactory.OrElse(
                    sqlExpressionFactory.IsNull(argument),
                    sqlExpressionFactory.Equal(CreateFunction("TRIM", typeof(string), argument.TypeMapping, argument), sqlExpressionFactory.Constant(string.Empty, argument.TypeMapping)));
            }

            return null;
        }

        var parameters = method.GetParameters();
        var stringArgs = parameters.All(p => p.ParameterType == typeof(string));

        switch (method.Name)
        {
            case nameof(string.ToUpper) when arguments.Count == 0:
                return CreateFunction("UPPER", typeof(string), instance.TypeMapping, instance);

            case nameof(string.ToLower) when arguments.Count == 0:
                return CreateFunction("LOWER", typeof(string), instance.TypeMapping, instance);

            case nameof(string.Trim) when arguments.Count == 0:
                return CreateFunction("TRIM", typeof(string), instance.TypeMapping, instance);

            case nameof(string.TrimStart) when arguments.Count == 0:
                return CreateFunction("LTRIM", typeof(string), instance.TypeMapping, instance);

            case nameof(string.TrimEnd) when arguments.Count == 0:
                return CreateFunction("RTRIM", typeof(string), instance.TypeMapping, instance);

            case nameof(string.Substring) when arguments.Count is 1 or 2 && parameters.All(p => p.ParameterType == typeof(int)):
            {
                var start = sqlExpressionFactory.Add(arguments[0], sqlExpressionFactory.Constant(1));
                return arguments.Count == 1
                    ? CreateFunction("SUBSTR", typeof(string), instance.TypeMapping, instance, start)
                    : CreateFunction("SUBSTR", typeof(string), instance.TypeMapping, instance, start, arguments[1]);
            }

            case nameof(string.Replace) when arguments.Count == 2 && stringArgs:
            {
                var mapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(instance, arguments[0], arguments[1]);
                return CreateFunction(
                    "REPLACE", typeof(string), mapping,
                    sqlExpressionFactory.ApplyTypeMapping(instance, mapping)!, sqlExpressionFactory.ApplyTypeMapping(arguments[0], mapping)!, sqlExpressionFactory.ApplyTypeMapping(arguments[1], mapping)!);
            }

            case nameof(string.IndexOf) when arguments.Count == 1 && stringArgs:
            {
                var mapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(instance, arguments[0]);
                var locate = CreateFunction("LOCATE", typeof(int), null, sqlExpressionFactory.ApplyTypeMapping(arguments[0], mapping)!, sqlExpressionFactory.ApplyTypeMapping(instance, mapping)!);
                return sqlExpressionFactory.Subtract(locate, sqlExpressionFactory.Constant(1));
            }

            case nameof(string.Contains) when arguments.Count == 1 && stringArgs:
            {
                var mapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(instance, arguments[0]);
                var pattern = sqlExpressionFactory.ApplyTypeMapping(arguments[0], mapping)!;
                if (pattern is SqlConstantExpression { Value: string text })
                {
                    return sqlExpressionFactory.Like(sqlExpressionFactory.ApplyTypeMapping(instance, mapping)!, sqlExpressionFactory.Constant($"%{EscapeLike(text)}%", mapping), sqlExpressionFactory.Constant(LikeEscapeChar.ToString()));
                }

                return sqlExpressionFactory.GreaterThan(
                    CreateFunction("LOCATE", typeof(int), null, pattern, sqlExpressionFactory.ApplyTypeMapping(instance, mapping)!),
                    sqlExpressionFactory.Constant(0));
            }

            case nameof(string.StartsWith) when arguments.Count == 1 && stringArgs:
                return TranslateStartsEndsWith(instance, arguments[0], startsWith: true);

            case nameof(string.EndsWith) when arguments.Count == 1 && stringArgs:
                return TranslateStartsEndsWith(instance, arguments[0], startsWith: false);
        }

        return null;
    }

    private static string EscapeLike(string text)
        => text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    /// <summary>
    /// Translates StartsWith/EndsWith: to LIKE for a constant pattern, otherwise to
    /// <c>LEFT</c>/<c>RIGHT(instance, LENGTH(pattern)) = pattern</c>, which needs no escaping of the pattern.
    /// </summary>
    /// <param name="instance">The string being tested.</param>
    /// <param name="pattern">The prefix or suffix.</param>
    /// <param name="startsWith">True for StartsWith, false for EndsWith.</param>
    /// <returns>The translated condition.</returns>
    private SqlExpression TranslateStartsEndsWith(SqlExpression instance, SqlExpression pattern, bool startsWith)
    {
        var mapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(instance, pattern);
        var mappedInstance = sqlExpressionFactory.ApplyTypeMapping(instance, mapping)!;
        var mappedPattern = sqlExpressionFactory.ApplyTypeMapping(pattern, mapping)!;

        if (mappedPattern is SqlConstantExpression { Value: string text })
        {
            var like = startsWith ? EscapeLike(text) + "%" : "%" + EscapeLike(text);
            return sqlExpressionFactory.Like(mappedInstance, sqlExpressionFactory.Constant(like, mapping), sqlExpressionFactory.Constant(LikeEscapeChar.ToString()));
        }

        var length = CreateFunction("LENGTH", typeof(int), null, mappedPattern);
        return sqlExpressionFactory.Equal(CreateFunction(startsWith ? "LEFT" : "RIGHT", typeof(string), mapping, mappedInstance, length), mappedPattern);
    }

    private SqlExpression CreateFunction(string name, Type type, RelationalTypeMapping? mapping, params SqlExpression[] arguments)
        => sqlExpressionFactory.Function(name, arguments, nullable: true, argumentsPropagateNullability: arguments.Select(_ => true), type, mapping);
}

/// <summary>Translates <see cref="string.Length"/>.</summary>
public class MapepireStringMemberTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMemberTranslator
{
    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        => instance is not null && member.DeclaringType == typeof(string) && member.Name == nameof(string.Length)
            ? sqlExpressionFactory.Function("LENGTH", [instance], nullable: true, argumentsPropagateNullability: [true], typeof(int))
            : null;
}

/// <summary>Translates <see cref="Math"/> methods.</summary>
public class MapepireMathTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMethodCallTranslator
{
    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(Math) && method.DeclaringType != typeof(MathF))
        {
            return null;
        }

        var name = method.Name switch
        {
            nameof(Math.Abs) when arguments.Count == 1 => "ABS",
            nameof(Math.Ceiling) when arguments.Count == 1 => "CEILING",
            nameof(Math.Floor) when arguments.Count == 1 => "FLOOR",
            nameof(Math.Pow) when arguments.Count == 2 => "POWER",
            nameof(Math.Sqrt) when arguments.Count == 1 => "SQRT",
            nameof(Math.Exp) when arguments.Count == 1 => "EXP",
            nameof(Math.Log) when arguments.Count == 1 => "LN",
            nameof(Math.Log10) when arguments.Count == 1 => "LOG10",
            nameof(Math.Sign) when arguments.Count == 1 => "SIGN",
            nameof(Math.Sin) when arguments.Count == 1 => "SIN",
            nameof(Math.Cos) when arguments.Count == 1 => "COS",
            nameof(Math.Tan) when arguments.Count == 1 => "TAN",
            nameof(Math.Round) when arguments.Count is 1 or 2 && method.GetParameters().All(p => p.ParameterType != typeof(MidpointRounding)) => "ROUND",
            nameof(Math.Truncate) when arguments.Count == 1 => "TRUNCATE",
            _ => null,
        };

        if (name is null)
        {
            return null;
        }

        var args = arguments.ToList();
        if (name is "ROUND" or "TRUNCATE" && args.Count == 1)
        {
            args.Add(sqlExpressionFactory.ApplyDefaultTypeMapping(sqlExpressionFactory.Constant(0))!);
        }

        var typeMapping = name is "SIGN" ? null : args[0].TypeMapping;
        return sqlExpressionFactory.Function(name, args, nullable: true, argumentsPropagateNullability: args.Select(_ => true), method.ReturnType, typeMapping);
    }
}

/// <summary>Translates <see cref="DateTime"/> / <see cref="DateOnly"/> members.</summary>
public class MapepireDateTimeMemberTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMemberTranslator
{
    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        var declaringType = member.DeclaringType;
        if (declaringType != typeof(DateTime) && declaringType != typeof(DateOnly) && declaringType != typeof(TimeOnly))
        {
            return null;
        }

        if (instance is null)
        {
            return member.Name switch
            {
                nameof(DateTime.Now) when declaringType == typeof(DateTime)
                    => sqlExpressionFactory.NiladicFunction("CURRENT TIMESTAMP", nullable: false, returnType),
                nameof(DateTime.UtcNow) when declaringType == typeof(DateTime)
                    => sqlExpressionFactory.Subtract(
                        sqlExpressionFactory.NiladicFunction("CURRENT TIMESTAMP", nullable: false, returnType),
                        sqlExpressionFactory.NiladicFunction("CURRENT TIMEZONE", nullable: false, typeof(decimal))),
                nameof(DateTime.Today) when declaringType == typeof(DateTime)
                    => sqlExpressionFactory.Function(
                        "TIMESTAMP",
                        [sqlExpressionFactory.NiladicFunction("CURRENT DATE", nullable: false, typeof(DateTime)), sqlExpressionFactory.ApplyDefaultTypeMapping(sqlExpressionFactory.Constant("00:00:00"))!],
                        nullable: false,
                        argumentsPropagateNullability: [false, false],
                        returnType),
                _ => null,
            };
        }

        var part = member.Name switch
        {
            nameof(DateTime.Year) => "YEAR",
            nameof(DateTime.Month) => "MONTH",
            nameof(DateTime.Day) => "DAY",
            nameof(DateTime.Hour) => "HOUR",
            nameof(DateTime.Minute) => "MINUTE",
            nameof(DateTime.Second) => "SECOND",
            nameof(DateTime.DayOfYear) => "DAYOFYEAR",
            _ => null,
        };

        if (part is not null)
        {
            return sqlExpressionFactory.Function(part, [instance], nullable: true, argumentsPropagateNullability: [true], returnType);
        }

        if (member.Name == nameof(DateTime.Date) && declaringType == typeof(DateTime))
        {
            return sqlExpressionFactory.Function(
                "TIMESTAMP",
                [sqlExpressionFactory.Function("DATE", [instance], nullable: true, argumentsPropagateNullability: [true], typeof(DateTime)), sqlExpressionFactory.ApplyDefaultTypeMapping(sqlExpressionFactory.Constant("00:00:00"))!],
                nullable: true,
                argumentsPropagateNullability: [true, false],
                returnType,
                instance.TypeMapping);
        }

        return null;
    }
}
