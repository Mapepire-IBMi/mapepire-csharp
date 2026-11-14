using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi.types.jdbcOptions;

namespace UnitTests;

[TestClass]
public sealed class SqlJobTests
{
    [TestMethod]
    public void UnderCommitControlReturnsFalseWhenIsolationIsNotConfigured()
    {
        SqlJob job = new();

        Assert.IsFalse(job.UnderCommitControl());
    }

    [TestMethod]
    public void UnderCommitControlReturnsFalseWhenIsolationIsNone()
    {
        JDBCOptions options = new();
        options.SetTransactionIsolation(
            TransactionIsolation.FromValue(TransactionIsolation.NONE));
        SqlJob job = new(options);

        Assert.IsFalse(job.UnderCommitControl());
    }

    [TestMethod]
    public void UnderCommitControlReturnsTrueWhenIsolationIsConfigured()
    {
        JDBCOptions options = new();
        options.SetTransactionIsolation(
            TransactionIsolation.FromValue(TransactionIsolation.READ_COMMITTED));
        SqlJob job = new(options);

        Assert.IsTrue(job.UnderCommitControl());
    }
}
