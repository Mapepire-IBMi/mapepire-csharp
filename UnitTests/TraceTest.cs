using System;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;

namespace UnitTests;



[TestClass]
public sealed class  TraceTest

{
    private int time;
    private String? invalidQuery;

    
    [TestInitialize]
    public void TestInitialize()
    {
       time = 0x7fffffff & DateTime.Now.GetHashCode();
       
       invalidQuery = "SELECT * FROM SAMPLE."+time;
 
    }

    [TestCleanup]
    public void TestCleanup()
    {
  
       DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);
        
        job.SetTraceLevel(ServerTraceLevel.OFF);

        job.Close();

  
    }
  
  
    [TestMethod]
    public void ServerTracingOff()  {
        AssertTraceData(invalidQuery, ServerTraceLevel.OFF, false);
    }

    [TestMethod]
    public void ServerTracingOn()  {
        AssertTraceData(invalidQuery, ServerTraceLevel.ON, true);
    }

    [TestMethod]
    public void ErrorServerTracing()  {
        AssertTraceData(invalidQuery, ServerTraceLevel.ERRORS, true);
    }

    [TestMethod]
    public void DataStreamServerTracing()  {
        AssertTraceData(invalidQuery, ServerTraceLevel.DATASTREAM, true);
    }

    public void AssertTraceData(String? sql, ServerTraceLevel level, bool traceExists)  {
        if (sql == null) throw new Exception("null sql string"); 
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);
     

        job.SetTraceLevel(level);
        Query query = job.Query(sql);

            try {
                query.Execute(1);
            } catch (Exception ex) {
                 String expectedMessage = "[SQL0104] Token ."+time+" was not valid. Valid tokens: FOR USE SKIP WAIT WITH FETCH LIMIT ORDER UNION EXCEPT.";
                Assert.AreEqual(expectedMessage, ex.Message);
            }
            query.Close(); 
        

        GetTraceDataResult result = job.GetTraceDataResult();

        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Id);

        if (traceExists) {
            Assert.IsTrue(result.TraceData
                    .IndexOf("com.ibm.as400.access.AS400JDBCSQLSyntaxErrorException: [SQL0104] Token ." + time) >= 0);
        } else {
            Assert.IsFalse(result.TraceData
                    .IndexOf("com.ibm.as400.access.AS400JDBCSQLSyntaxErrorException: [SQL0104] Token ." + time) >= 0);
        }

        
    }
}
