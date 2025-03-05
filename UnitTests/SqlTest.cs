using System;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi.types.jdbcOptions;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace UnitTests;



[TestClass]
public sealed class  SQLTest

{
    
    [TestInitialize]
    public void TestInitialize()
    {
    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    
    [TestMethod]
    public void SimpleQuery()
    {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);
        Query query = job.Query("SELECT * FROM SAMPLE.DEPARTMENT");
        QueryResult result = query.Execute();

        query.Close();
        job.Close();
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsDone);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Metadata);
        Assert.IsNotNull(result.Data); 
        Assert.IsTrue(result.Data.Count > 0);
    }



    [TestMethod]
    public void SimpleQueryWithJDBCOptions()  {
        JDBCOptions options = new();
        options.SetLibraries (["SAMPLE"]);
        options.SetNaming(Naming.SQL);
        SqlJob job = new(options);

        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
		ConnectionResult? cr = job.Connect(daemonServer);
      
      


        Query queryA = job.Query("SELECT * FROM DEPARTMENT");
        QueryResult result = queryA.Execute();

        try { 
              Query queryB = job.Query("SELECT * FROM SAMPLE/DEPARTMENT");
              queryB.Execute(1); 
              Assert.Fail("show have thrown exception from system naming"); 
            } catch (Exception ex) {
                String expectedMessage = "[SQL5016] Qualified object name DEPARTMENT not valid.";
                Assert.AreEqual(expectedMessage, ex.Message);
            }
        

        queryA.Close();
        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsDone);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Metadata);
        Assert.IsNotNull(result.Data); 
        Assert.IsTrue(result.Data.Count > 0);
        
    }

    [TestMethod]
    public void SimpleQueryInTerseFormat()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        QueryOptions options = new QueryOptions(true, false, null);
        Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS", options);
        QueryResult result = query.Execute(5);
        Assert.IsNotNull(result.Data);
        Dictionary<string, object> row = result.Data[0];
        query.Close();
        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Metadata);
        Assert.IsFalse(result.IsDone);
        Assert.AreEqual(5, result.Data.Count);
        Assert.IsNotNull(result.Metadata); 
        Assert.IsNotNull(result.Metadata.Columns); 
        Assert.IsNotNull(result.Metadata.Columns[0].Name); 
        String? firstColumnName=result.Metadata.Columns[0].Name;
        Assert.IsNotNull(row[firstColumnName]);
    }

    [TestMethod]
    public void LargeDatasetQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS");
        QueryResult result = query.Execute(50);

        query.Close();
        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Metadata);
        Assert.IsFalse(result.IsDone);
        Assert.IsNotNull(result.Data); 
        Assert.AreEqual(50, result.Data.Count);
    }

    [TestMethod]
    public void NotExistentTableQuery()  {
        DaemonServer? daemonServer = MapepireTest.GetTestDaemonServer(); 
        Assert.IsNotNull(daemonServer); 
        Assert.IsNotNull(daemonServer.User); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Query query = job.Query("SELECT * from SCOOBY");
    Exception? e = null; 
            try {
                query.Execute(1);
            } catch (Exception ex) {
                query.Close();
                job.Close();
                e = ex;
            }
        
        Assert.IsNotNull(e);    
        Assert.IsTrue(e.ToString()
                .Contains("[SQL0204] SCOOBY in " + daemonServer.User.ToUpper() + " type *FILE not found."));
    }

    [TestMethod]
    public void EmptyQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Exception? e = null; 
            Query query = job.Query("");

            try {
                query.Execute(1);
            } catch (Exception ex) {
                query.Close();
                job.Close();
                e = ex; 
             }
        

        Assert.IsNotNull(e); 
        Assert.IsTrue(e.ToString().Contains("A string parameter value with zero length was detected."));
    }

    [TestMethod]
    public void InvalidTokenQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Exception? e = null; 
            Query query = job.Query("a");

            try {
                query.Execute(1);
            } catch (Exception ex) {
                query.Close();
                job.Close();
                e = ex; 
            }
        

        Assert.IsNotNull(e); 
        Assert.IsNotNull(e.ToString()); 
        Assert.IsTrue(e.ToString().Contains(
                "[SQL0104] Token A was not valid. Valid tokens: ( CL END GET SET TAG CALL DROP FREE HOLD LOCK OPEN WITH ALTER."));
    }

    [TestMethod]
    public void InvalidRowToFetchQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Exception? e = null; 
            Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS");

            try {
                query.Execute(0);
            } catch (Exception ex) {
                query.Close();
                job.Close();
                e = ex;
            }
      
    Assert.IsNotNull(e); 
        Assert.AreEqual("Rows to fetch must be greater than 0", e.Message);
    }

    [TestMethod]
    public void DropTableQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Query query = job.Query("DROP TABLE SAMPLE.DELETE IF EXISTS");
        QueryResult result = query.Execute();

        query.Close();
        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Id);
    }

    [TestMethod]
    public void FetchMoreQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS");
        QueryResult result = query.Execute(5);
        Assert.IsNotNull(result); 
        while (!result.IsDone) {
            Assert.IsNotNull(result.Data); 
            Assert.IsTrue(result.Data.Count > 0);
            result = query.FetchMore(300);
        }

        query.Close();
        job.Close();
    }

    [TestMethod]
    public void FetchMoreOnPreparedQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        QueryOptions options = new QueryOptions(false, false, ["N"]);
        Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS WHERE IS_NULLABLE = ?", options);
        QueryResult result = query.Execute(5);

        while (!result.IsDone) {
            Assert.IsNotNull(result.Data); 
            Assert.IsTrue(result.Data.Count > 0);
            result = query.FetchMore(300);
        }

        query.Close();
        job.Close();
    }

    [TestMethod]
    public void ExecuteOnPreparedQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        QueryOptions options = new QueryOptions(false, false, ["LONG_COMMENT"]);
        Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS WHERE COLUMN_NAME = ?", options);
        QueryResult result = query.Execute(10);

        query.Close();
        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsDone);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Metadata);
        Assert.IsNotNull(result.Data); 
        Assert.IsTrue(result.Data.Count > 0);
    }

    [TestMethod]
    public void ExecuteOnPreparedQueryInTerseFormat()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        QueryOptions options = new QueryOptions(true, false,  ["TABLE_NAME"]);
        Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS WHERE COLUMN_NAME = ?", options);
        QueryResult result = query.Execute(1);
        Assert.IsNotNull(result.Data);
        Dictionary<string, object> row = result.Data[0];
        query.Close();
        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Metadata);
        Assert.IsFalse(result.IsDone);
        Assert.AreEqual(1, result.Data.Count);
        
        
            Assert.IsNotNull(result.Metadata); 
        Assert.IsNotNull(result.Metadata.Columns); 
        Assert.IsNotNull(result.Metadata.Columns[0].Name); 
        String? firstColumnName=result.Metadata.Columns[0].Name;
        Assert.IsNotNull(row[firstColumnName]);
        Assert.AreEqual("TABLE_NAME", row[firstColumnName].ToString());
    
    }

    [TestMethod]
    public void ExecuteOnMultipleParameterPreparedQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        QueryOptions options = new QueryOptions(false, false,
                ["TABLE_NAME", "LONG_COMMENT", "CONSTRAINT_NAME"]);
        Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS WHERE COLUMN_NAME IN (?, ?, ?)", options);
        QueryResult result = query.Execute(30);

        query.Close();
        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsDone);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Metadata);
        Assert.IsNotNull(result.Data); 
        Assert.IsTrue(result.Data.Count > 0);
    }

    [TestMethod]
    public void ExecuteOnNoParameterPreparedQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Exception? e = null; 
            QueryOptions options = new QueryOptions(false, false, []);
            Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS WHERE COLUMN_NAME = ?", options);

            try {
                query.Execute();
            } catch (Exception ex) {
                query.Close();
                job.Close();
                e=ex;
            }
        
        Assert.IsNotNull(e);    
        Assert.IsTrue(e.ToString().Contains(
                "The number of parameter values set or registered does not match the number of parameters."));
    }

    [TestMethod]
    public void ExecuteOnWrongParameterCountPreparedQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Exception? e = null; 
            QueryOptions options = new QueryOptions(false, false, ["A", "B"]);
            Query query = job.Query("SELECT * FROM SAMPLE.SYSCOLUMNS WHERE COLUMN_NAME = ?", options);

            try {
                query.Execute();
            } catch (Exception ex) {
                query.Close();
                job.Close();
                 e = ex;
            }
    
    Assert.IsNotNull(e);
        Assert.IsTrue(e.ToString().Contains("Descriptor index not valid. (2>1)"));
    }

    [TestMethod]
    public void ExecuteOnInvalidPreparedQuery()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        Exception? e = null; 
            QueryOptions options = new QueryOptions(false, false, ["FAKE_COLUMN"]);
            Query query = job.Query("SELECT * FROM FAKE_SCHEMA.FAKE_TABLE WHERE COLUMN_NAME = ?", options);

            try {
                query.Execute();
            } catch (Exception ex) {
                query.Close();
                job.Close();
                e = ex; 
            }
        
    Assert.IsNotNull(e); 
        Assert.IsTrue(e.ToString().Contains("[SQL0204] FAKE_TABLE in FAKE_SCHEMA type *FILE not found."));
    }

    [TestMethod]
    public void SimpleJobExecute()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        QueryResult result = job.Execute("SELECT * FROM SAMPLE.DEPARTMENT");

        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.IsDone);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.HasResults);
        Assert.IsNotNull(result.Metadata);
        Assert.IsNotNull(result.Data); 
        Assert.IsTrue(result.Data.Count > 0);
    }

    [TestMethod]
    public void SingleJobMultipleStatements()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        QueryResult resultA = job.Execute("SELECT * FROM SAMPLE.DEPARTMENT");
        QueryResult resultB = job.Execute("SELECT * FROM SAMPLE.EMPLOYEE");

        job.Close();

        Assert.IsTrue(resultA.IsDone);
        Assert.IsTrue(resultB.IsDone);
    }

 
}