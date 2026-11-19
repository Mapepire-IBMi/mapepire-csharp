using System.Text.Json;
using io.github.mapepire_ibmi.types;

namespace UnitTests;

/// <summary>Verifies how terse wire replies become keyed query results.</summary>
[TestClass]
public sealed class TerseQueryResultTests
{
    /// <summary>Checks that values use metadata names and retain their original JSON types.</summary>
    [TestMethod]
    public void ConvertsPositionalValuesByColumnName()
    {
        const string json = """
            {"id":"query1","success":true,"is_done":true,"has_results":true,
             "metadata":{"columns":[{"name":"N"},{"name":"TEXT"}]},
             "data":[[42,"hello"]]}
            """;
        TerseQueryResult? response = JsonSerializer.Deserialize<TerseQueryResult>(json);
        Assert.IsNotNull(response);

        QueryResult result = response.ConvertToQueryResult();

        Assert.AreEqual("query1", result.Id);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsDone);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Data);
        Dictionary<string, object> row = result.Data.Single();
        Assert.AreEqual(42, ((JsonElement)row["N"]).GetInt32());
        Assert.AreEqual("hello", ((JsonElement)row["TEXT"]).GetString());
        Assert.IsNull(result.OutputParms);
    }

    /// <summary>Checks that a failed reply without rows preserves its SQL error information.</summary>
    [TestMethod]
    public void PreservesErrorWithoutRowMetadata()
    {
        const string json = """
            {"id":"query2","success":false,"error":"Table not found","sql_state":"42704","sql_rc":-204}
            """;
        TerseQueryResult? response = JsonSerializer.Deserialize<TerseQueryResult>(json);
        Assert.IsNotNull(response);

        QueryResult result = response.ConvertToQueryResult();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("42704", result.SqlState);
        Assert.AreEqual(-204, result.SqlRc);
        Assert.AreEqual("Table not found", result.Error);
        Assert.IsNull(result.Metadata);
        Assert.IsNull(result.Data);
    }

    /// <summary>Checks that an explicit empty result stays empty when metadata is absent.</summary>
    [TestMethod]
    public void PreservesEmptyRowsWithoutMetadata()
    {
        TerseQueryResult? response = JsonSerializer.Deserialize<TerseQueryResult>("""{"success":true,"data":[]}""");
        Assert.IsNotNull(response);

        QueryResult result = response.ConvertToQueryResult();

        Assert.IsNotNull(result.Data);
        Assert.AreEqual(0, result.Data.Count);
    }

    /// <summary>Checks that positional values without usable names produce a protocol error.</summary>
    /// <param name="json">A reply with missing column metadata or names.</param>
    [TestMethod]
    [DataRow("""{"data":[[1]]}""")]
    [DataRow("""{"metadata":{"columns":[{"name":"N"}]},"data":[[1,2]]}""")]
    [DataRow("""{"metadata":{"columns":[{}]},"data":[[1]]}""")]
    public void RejectsRowsWithoutColumnNames(string json)
    {
        TerseQueryResult? response = JsonSerializer.Deserialize<TerseQueryResult>(json);
        Assert.IsNotNull(response);

        Assert.ThrowsException<JsonException>(() => response.ConvertToQueryResult());
    }
}
