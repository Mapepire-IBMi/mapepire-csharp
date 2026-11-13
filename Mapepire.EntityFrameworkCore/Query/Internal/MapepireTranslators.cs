namespace Mapepire.EntityFrameworkCore.Query.Internal;

/// <summary>Registers the Db2 for i method call translators.</summary>
public class MapepireMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
{
    public MapepireMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        var f = dependencies.SqlExpressionFactory;
        AddTranslators(
        [
            new MapepireStringMethodTranslator(f),
            new MapepireMathTranslator(f),
        ]);
    }
}

/// <summary>Registers the Db2 for i member translators.</summary>
public class MapepireMemberTranslatorProvider : RelationalMemberTranslatorProvider
{
    public MapepireMemberTranslatorProvider(RelationalMemberTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        var f = dependencies.SqlExpressionFactory;
        AddTranslators(
        [
            new MapepireStringMemberTranslator(f),
            new MapepireDateTimeMemberTranslator(f),
        ]);
    }
}

/// <summary>Translates <see cref="string"/> instance and static methods.</summary>
public class MapepireStringMethodTranslator(ISqlExpressionFactory f) : IMethodCallTranslator
{
    private const char LikeEscapeChar = '\\';

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

        if (instance == null)
        {
            if (method.Name == nameof(string.IsNullOrWhiteSpace) && arguments.Count == 1)
            {
                var arg = arguments[0];
                return f.OrElse(
                    f.IsNull(arg),
                    f.Equal(Fn("TRIM", typeof(string), arg.TypeMapping, arg), f.Constant(string.Empty, arg.TypeMapping)));
            }

            return null;
        }

        var parameters = method.GetParameters();
        var stringArgs = parameters.All(p => p.ParameterType == typeof(string));

        switch (method.Name)
        {
            case nameof(string.ToUpper) when arguments.Count == 0:
                return Fn("UPPER", typeof(string), instance.TypeMapping, instance);

            case nameof(string.ToLower) when arguments.Count == 0:
                return Fn("LOWER", typeof(string), instance.TypeMapping, instance);

            case nameof(string.Trim) when arguments.Count == 0:
                return Fn("TRIM", typeof(string), instance.TypeMapping, instance);

            case nameof(string.TrimStart) when arguments.Count == 0:
                return Fn("LTRIM", typeof(string), instance.TypeMapping, instance);

            case nameof(string.TrimEnd) when arguments.Count == 0:
                return Fn("RTRIM", typeof(string), instance.TypeMapping, instance);

            case nameof(string.Substring) when arguments.Count is 1 or 2 && parameters.All(p => p.ParameterType == typeof(int)):
            {
                var start = f.Add(arguments[0], f.Constant(1));
                return arguments.Count == 1
                    ? Fn("SUBSTR", typeof(string), instance.TypeMapping, instance, start)
                    : Fn("SUBSTR", typeof(string), instance.TypeMapping, instance, start, arguments[1]);
            }

            case nameof(string.Replace) when arguments.Count == 2 && stringArgs:
            {
                var mapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(instance, arguments[0], arguments[1]);
                return Fn(
                    "REPLACE", typeof(string), mapping,
                    f.ApplyTypeMapping(instance, mapping)!, f.ApplyTypeMapping(arguments[0], mapping)!, f.ApplyTypeMapping(arguments[1], mapping)!);
            }

            case nameof(string.IndexOf) when arguments.Count == 1 && stringArgs:
            {
                var mapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(instance, arguments[0]);
                var locate = Fn("LOCATE", typeof(int), null, f.ApplyTypeMapping(arguments[0], mapping)!, f.ApplyTypeMapping(instance, mapping)!);
                return f.Subtract(locate, f.Constant(1));
            }

            case nameof(string.Contains) when arguments.Count == 1 && stringArgs:
            {
                var mapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(instance, arguments[0]);
                var pattern = f.ApplyTypeMapping(arguments[0], mapping)!;
                if (pattern is SqlConstantExpression { Value: string s })
                {
                    return f.Like(f.ApplyTypeMapping(instance, mapping)!, f.Constant($"%{EscapeLike(s)}%", mapping), f.Constant(LikeEscapeChar.ToString()));
                }

                return f.GreaterThan(
                    Fn("LOCATE", typeof(int), null, pattern, f.ApplyTypeMapping(instance, mapping)!),
                    f.Constant(0));
            }

            case nameof(string.StartsWith) when arguments.Count == 1 && stringArgs:
                return TranslateStartsEndsWith(instance, arguments[0], startsWith: true);

            case nameof(string.EndsWith) when arguments.Count == 1 && stringArgs:
                return TranslateStartsEndsWith(instance, arguments[0], startsWith: false);
        }

        return null;
    }

    private SqlExpression TranslateStartsEndsWith(SqlExpression instance, SqlExpression pattern, bool startsWith)
    {
        var mapping = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions.InferTypeMapping(instance, pattern);
        instance = f.ApplyTypeMapping(instance, mapping)!;
        pattern = f.ApplyTypeMapping(pattern, mapping)!;

        if (pattern is SqlConstantExpression { Value: string s })
        {
            var like = startsWith ? EscapeLike(s) + "%" : "%" + EscapeLike(s);
            return f.Like(instance, f.Constant(like, mapping), f.Constant(LikeEscapeChar.ToString()));
        }

        // LEFT/RIGHT(instance, LENGTH(pattern)) = pattern
        var length = Fn("LENGTH", typeof(int), null, pattern);
        return f.Equal(Fn(startsWith ? "LEFT" : "RIGHT", typeof(string), mapping, instance, length), pattern);
    }

    private static string EscapeLike(string s)
        => s.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    private SqlExpression Fn(string name, Type type, RelationalTypeMapping? mapping, params SqlExpression[] args)
        => f.Function(name, args, nullable: true, argumentsPropagateNullability: args.Select(_ => true), type, mapping);
}

/// <summary>Translates <see cref="string.Length"/>.</summary>
public class MapepireStringMemberTranslator(ISqlExpressionFactory f) : IMemberTranslator
{
    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        => instance != null && member.DeclaringType == typeof(string) && member.Name == nameof(string.Length)
            ? f.Function("LENGTH", [instance], nullable: true, argumentsPropagateNullability: [true], typeof(int))
            : null;
}

/// <summary>Translates <see cref="Math"/> methods.</summary>
public class MapepireMathTranslator(ISqlExpressionFactory f) : IMethodCallTranslator
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

        if (name == null)
        {
            return null;
        }

        var args = arguments.ToList();
        if (name is "ROUND" or "TRUNCATE" && args.Count == 1)
        {
            args.Add(f.ApplyDefaultTypeMapping(f.Constant(0))!);
        }

        var typeMapping = name is "SIGN" ? null : args[0].TypeMapping;
        return f.Function(name, args, nullable: true, argumentsPropagateNullability: args.Select(_ => true), method.ReturnType, typeMapping);
    }
}

/// <summary>Translates <see cref="DateTime"/> / <see cref="DateOnly"/> members.</summary>
public class MapepireDateTimeMemberTranslator(ISqlExpressionFactory f) : IMemberTranslator
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

        if (instance == null)
        {
            return member.Name switch
            {
                nameof(DateTime.Now) when declaringType == typeof(DateTime)
                    => f.NiladicFunction("CURRENT TIMESTAMP", nullable: false, returnType),
                nameof(DateTime.UtcNow) when declaringType == typeof(DateTime)
                    => f.Subtract(
                        f.NiladicFunction("CURRENT TIMESTAMP", nullable: false, returnType),
                        f.NiladicFunction("CURRENT TIMEZONE", nullable: false, typeof(decimal))),
                nameof(DateTime.Today) when declaringType == typeof(DateTime)
                    => f.Function(
                        "TIMESTAMP",
                        [f.NiladicFunction("CURRENT DATE", nullable: false, typeof(DateTime)), f.ApplyDefaultTypeMapping(f.Constant("00:00:00"))!],
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

        if (part != null)
        {
            return f.Function(part, [instance], nullable: true, argumentsPropagateNullability: [true], returnType);
        }

        if (member.Name == nameof(DateTime.Date) && declaringType == typeof(DateTime))
        {
            return f.Function(
                "TIMESTAMP",
                [f.Function("DATE", [instance], nullable: true, argumentsPropagateNullability: [true], typeof(DateTime)), f.ApplyDefaultTypeMapping(f.Constant("00:00:00"))!],
                nullable: true,
                argumentsPropagateNullability: [true, false],
                returnType,
                instance.TypeMapping);
        }

        return null;
    }
}
