using System.Data;
using Mapepire.Data;
using Mapepire.Data.Internal;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Offline tests for the ADO.NET layer.</summary>
[TestClass]
public sealed class AdoNetTests
{
    private static SqlParameterRewriter.Result Rewrite(string sql, params string[] names)
        => SqlParameterRewriter.Rewrite(sql, n => names.Contains(n, StringComparer.OrdinalIgnoreCase));

    [TestMethod]
    public void Named_parameters_become_positional_and_repeat()
    {
        var r = Rewrite("SELECT * FROM T WHERE A = @p0 OR B = @p1 OR C = @p0", "p0", "p1");
        Assert.AreEqual("SELECT * FROM T WHERE A = ? OR B = ? OR C = ?", r.Sql);
        CollectionAssert.AreEqual(new[] { "p0", "p1", "p0" }, r.NamedOrder!.ToArray());
    }

    [TestMethod]
    public void Markers_inside_literals_identifiers_and_comments_are_ignored()
    {
        var r = Rewrite("SELECT '@p0 ?', \"@p0\" FROM T -- @p0 ?\n/* @p0 ? */ WHERE X = @p0", "p0");
        Assert.AreEqual("SELECT '@p0 ?', \"@p0\" FROM T -- @p0 ?\n/* @p0 ? */ WHERE X = ?", r.Sql);
        Assert.AreEqual(1, r.NamedOrder!.Count);
        Assert.AreEqual(0, r.PositionalCount);
    }

    [TestMethod]
    public void Escaped_quotes_inside_literals()
    {
        var r = Rewrite("SELECT 'it''s @p0' FROM T WHERE X = @p0", "p0");
        Assert.AreEqual("SELECT 'it''s @p0' FROM T WHERE X = ?", r.Sql);
    }

    [TestMethod]
    public void At_sign_inside_identifier_and_unknown_names_are_kept()
    {
        var r = Rewrite("SELECT MY@COL, @unknown FROM T WHERE X = @p0", "p0");
        Assert.AreEqual("SELECT MY@COL, @unknown FROM T WHERE X = ?", r.Sql);
    }

    [TestMethod]
    public void Positional_markers_are_counted()
    {
        var r = Rewrite("INSERT INTO T VALUES (?, ?, '?')");
        Assert.IsNull(r.NamedOrder);
        Assert.AreEqual(2, r.PositionalCount);
    }

    [TestMethod]
    public void Mixed_markers_are_rejected()
        => Assert.ThrowsException<InvalidOperationException>(() => Rewrite("SELECT ? FROM T WHERE X = @p0", "p0"));

    [TestMethod]
    public void Trailing_terminator_is_removed()
        => Assert.AreEqual("SELECT 1 FROM SYSIBM.SYSDUMMY1", Rewrite("  SELECT 1 FROM SYSIBM.SYSDUMMY1;\r\n ").Sql);

    [TestMethod]
    public void Command_binds_named_parameters_in_order()
    {
        var command = new MapepireCommand("UPDATE T SET A = @a, B = @b WHERE A = @a;");
        command.Parameters.AddWithValue("@b", 2m);
        command.Parameters.AddWithValue("a", 1);
        var (sql, bound) = command.BuildStatement();
        Assert.AreEqual("UPDATE T SET A = ?, B = ? WHERE A = ?", sql);
        CollectionAssert.AreEqual(new object?[] { 1, "2", 1 }, bound.Values);
    }

    [TestMethod]
    public void Stored_procedure_command_builds_call()
    {
        var command = new MapepireCommand("MYLIB.MYPROC") { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("x", "a");
        command.Parameters.Add(new MapepireParameter("y", null) { Direction = ParameterDirection.Output });
        var (sql, bound) = command.BuildStatement();
        Assert.AreEqual("CALL MYLIB.MYPROC(?, ?)", sql);
        Assert.AreEqual(2, bound.Values.Count);
    }

    [TestMethod]
    public void Parameter_values_are_wire_safe()
    {
        Assert.AreEqual("2024-01-02 03:04:05.123456", WireValue.FromParameter(new DateTime(2024, 1, 2, 3, 4, 5).AddTicks(1234560), null));
        Assert.AreEqual("2024-01-02", WireValue.FromParameter(new DateTime(2024, 1, 2), DbType.Date));
        Assert.AreEqual("2024-01-02", WireValue.FromParameter(new DateOnly(2024, 1, 2), null));
        Assert.AreEqual("13:14:15", WireValue.FromParameter(new TimeOnly(13, 14, 15), null));
        Assert.AreEqual("12345678901234567.89", WireValue.FromParameter(12345678901234567.89m, null));
        Assert.AreEqual("9007199254740993", WireValue.FromParameter(9007199254740993L, null));
        Assert.AreEqual(1, WireValue.FromParameter(true, DbType.Int16));
        Assert.AreEqual(true, WireValue.FromParameter(true, DbType.Boolean));
        Assert.AreEqual("CAFE", WireValue.FromParameter(new byte[] { 0xCA, 0xFE }, null));
        Assert.IsNull(WireValue.FromParameter(DBNull.Value, null));
    }

    [TestMethod]
    public void Timestamps_in_db2_and_jdbc_formats_are_parsed()
    {
        var expected = new DateTime(2024, 1, 2, 3, 4, 5).AddTicks(1234560);
        Assert.AreEqual(expected, WireValue.ParseTimestamp("2024-01-02-03.04.05.123456"));
        Assert.AreEqual(expected, WireValue.ParseTimestamp("2024-01-02 03:04:05.123456"));
        Assert.AreEqual(expected, WireValue.ParseTimestamp("2024-01-02 03:04:05.123456000000"));
        Assert.AreEqual(new DateTime(2024, 1, 2), WireValue.ParseDate("2024-01-02"));
        Assert.AreEqual(new TimeSpan(3, 4, 5), WireValue.ParseTime("03.04.05"));
        Assert.AreEqual(new TimeSpan(3, 4, 5), WireValue.ParseTime("03:04:05"));
    }

    [TestMethod]
    public void Json_values_convert_by_column_type()
    {
        static System.Text.Json.JsonElement J(string json) => System.Text.Json.JsonDocument.Parse(json).RootElement;
        Assert.AreEqual(42, WireValue.ToClr(J("42"), Db2Type.FromName("INTEGER")));
        Assert.AreEqual(12.5m, WireValue.ToClr(J("12.50"), Db2Type.FromName("DECIMAL")));
        Assert.AreEqual(12.5m, WireValue.ToClr(J("\"12.50\""), Db2Type.FromName("NUMERIC")));
        Assert.AreEqual("abc  ", WireValue.ToClr(J("\"abc  \""), Db2Type.FromName("CHAR")));
        Assert.AreEqual(new DateTime(2024, 1, 2), WireValue.ToClr(J("\"2024-01-02\""), Db2Type.FromName("DATE")));
        Assert.AreEqual(DBNull.Value, WireValue.ToClr(J("null"), Db2Type.FromName("VARCHAR")));
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, (byte[])WireValue.ToClr(J("\"0102\""), Db2Type.FromName("VARBINARY")));
        Assert.AreEqual(typeof(byte[]), Db2Type.FromName("CHAR () FOR BIT DATA").ClrType);
    }

    [TestMethod]
    public void Connection_string_synonyms_and_jdbc_options()
    {
        var b = new MapepireConnectionStringBuilder("Host=sys1;UID=me;PWD=pw;Database=LIB1;Libraries=LIB2, LIB3;Auto Commit=false;Trust Server Certificate=true");
        Assert.AreEqual("sys1", b.Server);
        Assert.AreEqual(8076, b.Port);
        Assert.AreEqual("me", b.UserId);
        Assert.AreEqual("pw", b.Password);
        Assert.IsFalse(b.RejectUnauthorized);
        Assert.IsFalse(b.AutoCommit);
        CollectionAssert.AreEqual(new[] { "LIB1", "LIB2", "LIB3" }, b.GetLibraryList().ToArray());

        var options = ServerSession.BuildJdbcOptions(b);
        Assert.AreEqual("LIB1,LIB2,LIB3", options["libraries"]);
        Assert.AreEqual("false", options["auto commit"]);
        Assert.AreEqual("sql", options["naming"]);
        Assert.AreEqual("iso", options["date format"]);
    }

    [TestMethod]
    public void Opening_without_server_fails_clearly()
    {
        using var connection = new MapepireConnection("User ID=me");
        var ex = Assert.ThrowsException<InvalidOperationException>(connection.Open);
        StringAssert.Contains(ex.Message, "Server");
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }
}
