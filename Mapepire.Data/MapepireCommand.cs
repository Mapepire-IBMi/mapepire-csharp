using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mapepire.Data.Internal;

namespace Mapepire.Data;

/// <summary>
/// A SQL statement executed against Db2 for i through the Mapepire server.
/// </summary>
/// <remarks>
/// Parameters may be referenced either positionally with <c>?</c> (bound in collection order) or by
/// name with <c>@name</c>. Named markers are rewritten to positional markers before execution.
/// </remarks>
public sealed class MapepireCommand : DbCommand
{
    private string _commandText = string.Empty;
    private MapepireConnection? _connection;
    private readonly MapepireParameterCollection _parameters = new();

    public MapepireCommand()
    {
    }

    public MapepireCommand(string? commandText, MapepireConnection? connection = null)
    {
        CommandText = commandText;
        _connection = connection;
    }

    /// <inheritdoc />
    [AllowNull]
    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? string.Empty;
    }

    /// <summary>Accepted for compatibility; the Mapepire protocol has no statement timeout.</summary>
    public override int CommandTimeout { get; set; } = 30;

    /// <inheritdoc />
    public override CommandType CommandType { get; set; } = CommandType.Text;

    /// <inheritdoc />
    public override bool DesignTimeVisible { get; set; }

    /// <inheritdoc />
    public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

    /// <summary>Rows requested per round trip. Defaults to the connection's <c>Fetch Size</c>.</summary>
    public int? FetchSize { get; set; }

    public new MapepireConnection? Connection
    {
        get => _connection;
        set => _connection = value;
    }

    /// <inheritdoc />
    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = (MapepireConnection?)value;
    }

    public new MapepireParameterCollection Parameters => _parameters;

    /// <inheritdoc />
    protected override DbParameterCollection DbParameterCollection => _parameters;

    public new MapepireTransaction? Transaction { get; set; }

    /// <inheritdoc />
    protected override DbTransaction? DbTransaction
    {
        get => Transaction;
        set => Transaction = (MapepireTransaction?)value;
    }

    /// <summary>Not supported by the Mapepire protocol; does nothing.</summary>
    public override void Cancel()
    {
    }

    /// <inheritdoc />
    public override void Prepare()
    {
        // Statements with parameters are always prepared by the server.
    }

    /// <inheritdoc />
    protected override DbParameter CreateDbParameter() => new MapepireParameter();

    public new MapepireParameter CreateParameter() => new();

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        var connection = CheckConnection();
        var (sql, bound) = BuildStatement();
        var result = connection.Session.Execute(sql, bound.Values, 1);
        if (!result.IsDone && result.Id != null)
        {
            connection.Session.CloseCursor(result.Id);
        }

        PropagateOutputParameters(result, bound.Parameters);
        connection.OnCommandCompleted();
        return result.UpdateCount;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        using var reader = ExecuteDbDataReader(CommandBehavior.SingleRow);
        return reader.Read() && reader.FieldCount > 0 ? reader.GetValue(0) : null;
    }

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var connection = CheckConnection();
        var (sql, bound) = BuildStatement();
        var fetchSize = behavior.HasFlag(CommandBehavior.SingleRow) ? 1 : FetchSize ?? connection.FetchSize;
        var result = connection.Session.Execute(sql, bound.Values, fetchSize);
        PropagateOutputParameters(result, bound.Parameters);
        return new MapepireDataReader(this, connection, result, sql, fetchSize, behavior);
    }

    /// <inheritdoc />
    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(ExecuteNonQuery, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(ExecuteScalar, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => ExecuteDbDataReader(behavior), cancellationToken);
    }

    private MapepireConnection CheckConnection()
    {
        var connection = _connection ?? throw new InvalidOperationException("The command has no connection.");
        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("The connection is not open.");
        }

        if (string.IsNullOrWhiteSpace(_commandText))
        {
            throw new InvalidOperationException("The command text has not been set.");
        }

        return connection;
    }

    internal readonly record struct BoundParameters(List<object?> Values, List<MapepireParameter> Parameters);

    /// <summary>Produces the SQL sent to the server and the ordered list of parameter values.</summary>
    internal (string Sql, BoundParameters Bound) BuildStatement()
    {
        var ordered = new List<MapepireParameter>();
        string sql;

        switch (CommandType)
        {
            case CommandType.StoredProcedure:
            {
                var sb = new StringBuilder("CALL ").Append(_commandText.Trim()).Append('(');
                foreach (MapepireParameter p in _parameters)
                {
                    if (p.Direction == ParameterDirection.ReturnValue)
                    {
                        continue;
                    }

                    sb.Append(ordered.Count == 0 ? "?" : ", ?");
                    ordered.Add(p);
                }

                sql = sb.Append(')').ToString();
                break;
            }

            case CommandType.TableDirect:
                sql = "SELECT * FROM " + _commandText.Trim();
                break;

            default:
            {
                var rewritten = SqlParameterRewriter.Rewrite(_commandText, name => _parameters.IndexOf(name) >= 0);
                sql = rewritten.Sql;
                if (rewritten.NamedOrder != null)
                {
                    foreach (var name in rewritten.NamedOrder)
                    {
                        ordered.Add(_parameters[name]);
                    }
                }
                else if (rewritten.PositionalCount > 0)
                {
                    foreach (MapepireParameter p in _parameters)
                    {
                        if (p.Direction != ParameterDirection.ReturnValue)
                        {
                            ordered.Add(p);
                        }
                    }

                    if (ordered.Count < rewritten.PositionalCount)
                    {
                        throw new InvalidOperationException(
                            $"The command text contains {rewritten.PositionalCount} parameter markers but only {ordered.Count} parameters were supplied.");
                    }

                    if (ordered.Count > rewritten.PositionalCount)
                    {
                        ordered.RemoveRange(rewritten.PositionalCount, ordered.Count - rewritten.PositionalCount);
                    }
                }

                break;
            }
        }

        var values = new List<object?>(ordered.Count);
        foreach (var p in ordered)
        {
            values.Add(p.Direction == ParameterDirection.Output ? null : WireValue.FromParameter(p.Value, p.ExplicitDbType));
        }

        return (sql, new BoundParameters(values, ordered));
    }

    private static void PropagateOutputParameters(ServerResult result, List<MapepireParameter> ordered)
    {
        foreach (var output in result.OutputParameters)
        {
            var index = output.Index - 1;
            if (index < 0 || index >= ordered.Count)
            {
                continue;
            }

            var p = ordered[index];
            if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
            {
                p.Value = WireValue.ToClr(output.Value, Db2Type.FromName(output.TypeName));
            }
        }
    }
}
