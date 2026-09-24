using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi.types.jdbcOptions;
using System.Reflection;

namespace UnitTests;

[TestClass]
public sealed class PendingTransactionsTests
{
    private const string TestSchema = "MAPETEST";
    private const string TestTable = $"{TestSchema}.PENDING_TXN_TEST";

    [TestMethod]
    public void GetPendingTransactionsReturnsZeroWhenNoChangesArePending()
    {
        SqlJob sqlJob = new();

        try
        {
            ConnectionResult? connection = sqlJob.Connect(MapepireTest.GetTestDaemonServer());

            Assert.IsNotNull(connection);
            Assert.IsTrue(connection.Success, connection.Error);
            Assert.AreEqual(0, sqlJob.GetPendingTransactions());
        }
        finally
        {
            sqlJob.Close();
        }
    }

    [TestMethod]
    public void GetPendingTransactionsReturnsOneForUncommittedInsert()
    {
        EnsureTestTableExists();

        JDBCOptions options = new();
        options.SetAutoCommit(false);
        options.SetTransactionIsolation(
            TransactionIsolation.FromValue(TransactionIsolation.READ_COMMITTED));

        SqlJob sqlJob = new(options);
        bool rollbackRequired = false;

        try
        {
            ConnectionResult? connection = sqlJob.Connect(MapepireTest.GetTestDaemonServer());

            Assert.IsNotNull(connection);
            Assert.IsTrue(connection.Success, connection.Error);
            Assert.AreEqual(0, sqlJob.GetPendingTransactions());

            string id = Guid.NewGuid().ToString("N");
            rollbackRequired = true;
            sqlJob.Execute($"INSERT INTO {TestTable} (ID) VALUES ('{id}')");

            Assert.AreEqual(1, sqlJob.GetPendingTransactions());

            sqlJob.EndTransaction(TransactionEndType.ROLLBACK);
            rollbackRequired = false;

            Assert.AreEqual(0, sqlJob.GetPendingTransactions());
        }
        finally
        {
            if (rollbackRequired)
            {
                try
                {
                    sqlJob.EndTransaction(TransactionEndType.ROLLBACK);
                }
                catch
                {
                    // Preserve the original test failure while still attempting cleanup.
                }
            }

            sqlJob.Close();
        }
    }

    [TestMethod]
    public void GetPendingTransactionsDoesNotRetainInternalQueryWhenExecutionFails()
    {
        SqlJob sqlJob = new();
        int trackedQueryCount = GetTrackedQueryCount(sqlJob);

        Assert.ThrowsException<Exception>(() => sqlJob.GetPendingTransactions());
        Assert.AreEqual(trackedQueryCount, GetTrackedQueryCount(sqlJob));
    }

    private static void EnsureTestTableExists()
    {
        SqlJob sqlJob = new();

        try
        {
            ConnectionResult? connection = sqlJob.Connect(MapepireTest.GetTestDaemonServer());

            Assert.IsNotNull(connection);
            Assert.IsTrue(connection.Success, connection.Error);

            ExecuteIgnoringAlreadyExists(sqlJob, $"CREATE SCHEMA {TestSchema}");
            ExecuteIgnoringAlreadyExists(
                sqlJob,
                $"CREATE TABLE {TestTable} (ID CHAR(32) NOT NULL PRIMARY KEY)");
        }
        finally
        {
            sqlJob.Close();
        }
    }

    private static void ExecuteIgnoringAlreadyExists(SqlJob sqlJob, string sql)
    {
        try
        {
            sqlJob.Execute(sql);
        }
        catch (SqlException exception) when (exception.SqlState == "42710")
        {
        }
    }

    private static int GetTrackedQueryCount(SqlJob sqlJob)
    {
        FieldInfo? queryListField = typeof(Query).GetField(
            "GlobalQueryList",
            BindingFlags.NonPublic | BindingFlags.Static);
        PropertyInfo? queryJobProperty = typeof(Query).GetProperty(
            "Job",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(queryListField);
        Assert.IsNotNull(queryJobProperty);

        List<Query>? trackedQueries = queryListField.GetValue(null) as List<Query>;
        Assert.IsNotNull(trackedQueries);

        lock (trackedQueries)
        {
            return trackedQueries.Count(query =>
                ReferenceEquals(queryJobProperty.GetValue(query), sqlJob));
        }
    }
}
