using System.Reflection;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;

namespace UnitTests;

[TestClass]
public sealed class InteractiveClientTests
{
    [TestMethod]
    public void QueryOutputAlignsValuesWithColumnNames()
    {
        List<ColumnMetadata> columns =
        [
            new ColumnMetadata { Name = "ID" },
            new ColumnMetadata { Name = "NAME" }
        ];
        List<Dictionary<string, object>> data =
        [
            new Dictionary<string, object> { ["ID"] = 1, ["NAME"] = "Ada" },
            new Dictionary<string, object> { ["ID"] = 200, ["NAME"] = "Grace Hopper" }
        ];

        string output = FormatQueryOutput(columns, data, false);

        string expected = string.Join(Environment.NewLine,
            "ID  NAME",
            "1   Ada",
            "200 Grace Hopper",
            "");
        Assert.AreEqual(expected, output);
    }

    [TestMethod]
    public void QueryOutputAlignsOptionalTypeRows()
    {
        List<ColumnMetadata> columns =
        [
            new ColumnMetadata { Name = "VALUE" },
            new ColumnMetadata { Name = "NOTE" }
        ];
        List<Dictionary<string, object>> data =
        [
            new Dictionary<string, object> { ["VALUE"] = 7, ["NOTE"] = "ok" }
        ];

        string output = FormatQueryOutput(columns, data, true);

        string expected = string.Join(Environment.NewLine,
            "VALUE        NOTE",
            "7            ok",
            "System.Int32 System.String",
            "");
        Assert.AreEqual(expected, output);
    }

    private static string FormatQueryOutput(List<ColumnMetadata> columns,
        List<Dictionary<string, object>> data, bool showTypes)
    {
        MethodInfo? formatter = typeof(InteractiveClient).GetMethod("FormatQueryOutput",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(formatter);

        return formatter.Invoke(null, [columns, data, showTypes]) as string
            ?? throw new AssertFailedException("InteractiveClient formatter returned null.");
    }
}
