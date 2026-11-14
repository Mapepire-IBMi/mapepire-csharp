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
    private readonly MapepireParameterCollection _parameters = new();
    private string _commandText = string.Empty;
    private MapepireConnection? _connection;

    /// <summary>Creates a command with no text and no connection.</summary>
    public MapepireCommand()
    {
    }

    /// <summary>Creates a command.</summary>
    /// <param name="commandText">The SQL statement.</param>
    /// <param name="connection">The connection to run it on.</param>
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

    /// <summary>The connection the command runs on.</summary>
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

    /// <summary>The command's parameters.</summary>
    public new MapepireParameterCollection Parameters => _parameters;

    /// <inheritdoc />
    protected override DbParameterCollection DbParameterCollection => _parameters;

    /// <summary>The transaction the command runs in, if any.</summary>
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

    /// <summary>Creates a parameter; it is not added to <see cref="Parameters"/>.</summary>
    public new MapepireParameter CreateParameter() => new();

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        var connection = CheckConnection();
        var (sql, bound) = BuildStatement();
        var result = connection.Session.Execute(sql, bound.Values, 1);
        if (!result.IsDone && result.Id is not null)
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

    /// <summary>
    /// Produces the SQL sent to the server and the ordered list of parameter values. Named markers are rewritten
    /// to positional ones; stored procedures become a CALL statement; TableDirect becomes SELECT *.
    /// </summary>
    /// <returns>The statement and its bound parameters, in marker order.</returns>
    /// <exception cref="InvalidOperationException">The statement has more positional markers than parameters.</exception>
    internal (string Sql, BoundParameters Bound) BuildStatement()
    {
        var ordered = new List<MapepireParameter>();
        string sql;

        switch (CommandType)
        {
            case CommandType.StoredProcedure:
            {
                var builder = new StringBuilder("CALL ").Append(_commandText.Trim()).Append('(');
                foreach (MapepireParameter parameter in _parameters)
                {
                    if (parameter.Direction == ParameterDirection.ReturnValue)
                    {
                        continue;
                    }

                    builder.Append(ordered.Count == 0 ? "?" : ", ?");
                    ordered.Add(parameter);
                }

                sql = builder.Append(')').ToString();
                break;
            }

            case CommandType.TableDirect:
                sql = "SELECT * FROM " + _commandText.Trim();
                break;

            default:
            {
                var rewritten = SqlParameterRewriter.Rewrite(_commandText, name => _parameters.IndexOf(name) >= 0);
                sql = rewritten.Sql;
                if (rewritten.NamedOrder is not null)
                {
                    foreach (var name in rewritten.NamedOrder)
                    {
                        ordered.Add(_parameters[name]);
                    }
                }
                else if (rewritten.PositionalCount > 0)
                {
                    foreach (MapepireParameter parameter in _parameters)
                    {
                        if (parameter.Direction != ParameterDirection.ReturnValue)
                        {
                            ordered.Add(parameter);
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
        foreach (var parameter in ordered)
        {
            values.Add(parameter.Direction == ParameterDirection.Output ? null : WireValue.FromParameter(parameter.Value, parameter.ExplicitDbType));
        }

        return (sql, new BoundParameters(values, ordered));
    }

    /// <inheritdoc />
    protected override DbParameter CreateDbParameter() => new MapepireParameter();

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var connection = CheckConnection();
        var (sql, bound) = BuildStatement();
        var fetchSize = behavior.HasFlag(CommandBehavior.SingleRow) ? 1 : FetchSize ?? connection.FetchSize;
        var result = connection.Session.Execute(sql, bound.Values, fetchSize);
        PropagateOutputParameters(result, bound.Parameters);
        return new MapepireDataReader(connection, result, sql, fetchSize, behavior);
    }

    /// <inheritdoc />
    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => ExecuteDbDataReader(behavior), cancellationToken);
    }

    /// <summary>Copies OUT and INOUT values returned by the server into their parameters.</summary>
    /// <param name="result">The server reply.</param>
    /// <param name="ordered">The parameters in marker order; the server reports values by 1-based marker index.</param>
    private static void PropagateOutputParameters(ServerResult result, List<MapepireParameter> ordered)
    {
        foreach (var output in result.OutputParameters)
        {
            var index = output.Index - 1;
            if (index < 0 || index >= ordered.Count)
            {
                continue;
            }

            var parameter = ordered[index];
            if (parameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
            {
                parameter.Value = WireValue.ToClr(output.Value, Db2Type.FromName(output.TypeName));
            }
        }
    }

    /// <summary>Checks that the command can run.</summary>
    /// <returns>The open connection.</returns>
    /// <exception cref="InvalidOperationException">There is no open connection, or no command text.</exception>
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

    /// <summary>Parameter values ready to send, and the parameters they came from, in marker order.</summary>
    internal readonly record struct BoundParameters(List<object?> Values, List<MapepireParameter> Parameters);
}
