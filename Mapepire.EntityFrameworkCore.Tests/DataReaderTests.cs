using System.Data;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>Reader completion behavior against the Mapepire mock server.</summary>
[TestClass]
public sealed class DataReaderTests
{
    /// <summary>Once a SingleRow reader returns false it must not fetch or expose another row.</summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SingleRowRemainsAtEndOfResults(bool useAsync)
    {
        await using var server = await MockMapepireServer.StartAsync(new
        {
            pattern = "select n from test_table",
            columns = new[] { new { name = "N", type = "INTEGER" } },
            rows = new[] { new[] { 1 }, new[] { 2 } },
        });
        var settings = new MapepireConnectionStringBuilder(server.ConnectionString) { AutoCommit = true, Pooling = false };
        using var connection = new MapepireConnection(settings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT N FROM TEST_TABLE";
        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        Assert.IsTrue(useAsync ? await reader.ReadAsync() : reader.Read());
        Assert.AreEqual(1, reader.GetInt32(0));
        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.IsFalse(useAsync ? await reader.ReadAsync() : reader.Read(), "The reader must stay at the end of its result.");
        }
    }
}
