using System.Data;
using Mapepire.Data;
using Mapepire.Data.Internal;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Offline tests for the ADO.NET layer.</summary>
[TestClass]
public sealed class AdoNetTests
{
    [TestMethod]
    public void NamedParametersBecomePositionalAndRepeat()
    {
        var r = Rewrite("SELECT * FROM T WHERE A = @p0 OR B = @p1 OR C = @p0", "p0", "p1");
        Assert.AreEqual("SELECT * FROM T WHERE A = ? OR B = ? OR C = ?", r.Sql);
        CollectionAssert.AreEqual(new[] { "p0", "p1", "p0" }, r.NamedOrder!.ToArray());
    }

    [TestMethod]
    public void MarkersInsideLiteralsIdentifiersAndCommentsAreIgnored()
    {
        var r = Rewrite("SELECT '@p0 ?', \"@p0\" FROM T -- @p0 ?\n/* @p0 ? */ WHERE X = @p0", "p0");
        Assert.AreEqual("SELECT '@p0 ?', \"@p0\" FROM T -- @p0 ?\n/* @p0 ? */ WHERE X = ?", r.Sql);
        Assert.AreEqual(1, r.NamedOrder!.Count);
        Assert.AreEqual(0, r.PositionalCount);
    }

    [TestMethod]
    public void EscapedQuotesInsideLiterals()
    {
        var r = Rewrite("SELECT 'it''s @p0' FROM T WHERE X = @p0", "p0");
        Assert.AreEqual("SELECT 'it''s @p0' FROM T WHERE X = ?", r.Sql);
    }

    [TestMethod]
    public void AtSignInsideIdentifierAndUnknownNamesAreKept()
    {
        var r = Rewrite("SELECT MY@COL, @unknown FROM T WHERE X = @p0", "p0");
        Assert.AreEqual("SELECT MY@COL, @unknown FROM T WHERE X = ?", r.Sql);
    }

    [TestMethod]
    public void PositionalMarkersAreCounted()
    {
        var r = Rewrite("INSERT INTO T VALUES (?, ?, '?')");
        Assert.IsNull(r.NamedOrder);
        Assert.AreEqual(2, r.PositionalCount);
    }

    [TestMethod]
    public void MixedMarkersAreRejected()
        => Assert.ThrowsException<InvalidOperationException>(() => Rewrite("SELECT ? FROM T WHERE X = @p0", "p0"));

    [TestMethod]
    public void TrailingTerminatorIsRemoved()
        => Assert.AreEqual("SELECT 1 FROM SYSIBM.SYSDUMMY1", Rewrite("  SELECT 1 FROM SYSIBM.SYSDUMMY1;\r\n ").Sql);

    /// <summary>A named parameter used twice is bound twice, in marker order, and the trailing terminator is dropped.</summary>
    [TestMethod]
    public void CommandBindsNamedParametersInOrder()
    {
        var command = new MapepireCommand("UPDATE T SET A = @a, B = @b WHERE A = @a;");
        command.Parameters.AddWithValue("@b", 2m);
        command.Parameters.AddWithValue("a", 1);
        var (sql, bound) = command.BuildStatement();
        Assert.AreEqual("UPDATE T SET A = ?, B = ? WHERE A = ?", sql);
        CollectionAssert.AreEqual(new object?[] { 1, "2", 1 }, bound.Values);
    }

    /// <summary>CommandType.StoredProcedure becomes a CALL with one marker per non-return-value parameter.</summary>
    [TestMethod]
    public void StoredProcedureCommandBuildsCall()
    {
        var command = new MapepireCommand("MYLIB.MYPROC") { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("x", "a");
        command.Parameters.Add(new MapepireParameter("y", null) { Direction = ParameterDirection.Output });
        var (sql, bound) = command.BuildStatement();
        Assert.AreEqual("CALL MYLIB.MYPROC(?, ?)", sql);
        Assert.AreEqual(2, bound.Values.Count);
    }

    /// <summary>Parameter values are converted to JSON-safe forms: JDBC date/time strings, decimals and 64-bit integers as strings, booleans per DbType, and bytes as hex.</summary>
    [TestMethod]
    public void ParameterValuesAreWireSafe()
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

    /// <summary>Server date/time values in the Db2 and JDBC formats parse, including over-long fractional seconds.</summary>
    [TestMethod]
    public void TimestampsInDb2AndJdbcFormatsAreParsed()
    {
        var expected = new DateTime(2024, 1, 2, 3, 4, 5).AddTicks(1234560);
        Assert.AreEqual(expected, WireValue.ParseTimestamp("2024-01-02-03.04.05.123456"));
        Assert.AreEqual(expected, WireValue.ParseTimestamp("2024-01-02 03:04:05.123456"));
        Assert.AreEqual(expected, WireValue.ParseTimestamp("2024-01-02 03:04:05.123456000000"));
        Assert.AreEqual(new DateTime(2024, 1, 2), WireValue.ParseDate("2024-01-02"));
        Assert.AreEqual(new TimeSpan(3, 4, 5), WireValue.ParseTime("03.04.05"));
        Assert.AreEqual(new TimeSpan(3, 4, 5), WireValue.ParseTime("03:04:05"));
    }

    /// <summary>JSON values from the server convert to the CLR type of their Db2 column type.</summary>
    [TestMethod]
    public void JsonValuesConvertByColumnType()
    {
        static System.Text.Json.JsonElement Json(string json) => System.Text.Json.JsonDocument.Parse(json).RootElement;
        Assert.AreEqual(42, WireValue.ToClr(Json("42"), Db2Type.FromName("INTEGER")));
        Assert.AreEqual(12.5m, WireValue.ToClr(Json("12.50"), Db2Type.FromName("DECIMAL")));
        Assert.AreEqual(12.5m, WireValue.ToClr(Json("\"12.50\""), Db2Type.FromName("NUMERIC")));
        Assert.AreEqual("abc  ", WireValue.ToClr(Json("\"abc  \""), Db2Type.FromName("CHAR")));
        Assert.AreEqual(new DateTime(2024, 1, 2), WireValue.ToClr(Json("\"2024-01-02\""), Db2Type.FromName("DATE")));
        Assert.AreEqual(DBNull.Value, WireValue.ToClr(Json("null"), Db2Type.FromName("VARCHAR")));
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, (byte[])WireValue.ToClr(Json("\"0102\""), Db2Type.FromName("VARBINARY")));
        Assert.AreEqual(typeof(byte[]), Db2Type.FromName("CHAR () FOR BIT DATA").ClrType);
    }

    /// <summary>Connection string synonyms resolve to canonical keywords and map onto the JDBC options sent to the server.</summary>
    [TestMethod]
    public void ConnectionStringSynonymsAndJdbcOptions()
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
    public void OpeningWithoutServerFailsClearly()
    {
        using var connection = new MapepireConnection("User ID=me");
        var ex = Assert.ThrowsException<InvalidOperationException>(connection.Open);
        StringAssert.Contains(ex.Message, "Server");
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    private static SqlParameterRewriter.Result Rewrite(string sql, params string[] names)
        => SqlParameterRewriter.Rewrite(sql, n => names.Contains(n, StringComparer.OrdinalIgnoreCase));

    [TestMethod]
    public void SettingsChangesDoNotChangeConnectionOrPoolIdentity()
    {
        const string connectionString = "Server=example.invalid;User ID=original;Password=unused;Auto Commit=false";
        using var connection = new MapepireConnection(connectionString);
        var settings = connection.Settings;
        settings.Server = "other.invalid";
        settings.UserId = "other";
        settings.Password = "other-unused";
        settings.AutoCommit = true;
        settings.DefaultSchema = "OTHER";

        Assert.AreEqual(connectionString, connection.ConnectionString);
        Assert.AreEqual("example.invalid", connection.DataSource);
        Assert.AreEqual("original", connection.Settings.UserId);
        Assert.AreEqual("unused", connection.Settings.Password);
        Assert.IsFalse(connection.Settings.AutoCommit);
        Assert.AreEqual(string.Empty, connection.Database);
    }

    [TestMethod]
    public void ConnectionStringChangesAreReflectedInNewSettingsSnapshots()
    {
        using var connection = new MapepireConnection("Server=first.invalid;User ID=first");
        var originalSettings = connection.Settings;
        connection.ConnectionString = "Server=second.invalid;User ID=second";

        Assert.AreEqual("first.invalid", originalSettings.Server);
        Assert.AreEqual("second.invalid", connection.Settings.Server);
        Assert.AreEqual("second", connection.Settings.UserId);
    }
}
