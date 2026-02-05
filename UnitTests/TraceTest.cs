using System;
using System.Diagnostics.Metrics;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;
using Microsoft.Testing.Extensions.VSTestBridge;

namespace UnitTests;



[TestClass]
public sealed class  TraceTest

{
  
    static Object lockObject = new(); 
    static DateTime startTime = DateTime.Now;
    static int counter = startTime.Hour * 3600000 + startTime.Minute *60000+ startTime.Second *1000+ startTime.Millisecond; 

    [TestInitialize]
    public void TestInitialize()
    {
  
    }

    [TestCleanup]
    public void TestCleanup()
    {
  
  
    }
  
  
    [TestMethod]
    public void ServerTracingOff()  {
       int counter =  getCounter();
       String invalidQuery = "SELECT * FROM SAMPLE."+counter;
       AssertTraceData(invalidQuery, counter, ServerTraceLevel.OFF, false);
    }

    [TestMethod]
    public void ServerTracingOn()  {
       int counter =  getCounter();
       String invalidQuery = "SELECT * FROM SAMPLE."+counter;
        AssertTraceData(invalidQuery, counter, ServerTraceLevel.ON, true);
    }

    [TestMethod]
    public void ErrorServerTracing()  {
       int counter =  getCounter();
       String invalidQuery = "SELECT * FROM SAMPLE."+counter;
        AssertTraceData(invalidQuery, counter, ServerTraceLevel.ERRORS, true);
    }

    public static int getCounter() { 
        
        lock(lockObject) { 
            counter++; 
        }
        return counter; 
    }
    [TestMethod]
    public void DataStreamServerTracing()  {
       int counter =  getCounter();
       string invalidQuery = "SELECT * FROM SAMPLE."+counter;
        AssertTraceData(invalidQuery, counter, ServerTraceLevel.DATASTREAM, true);
    }

    public void AssertTraceData(String? sql, int counter, ServerTraceLevel level, bool traceExists)  {
        lock(lockObject) { 
        if (sql == null) throw new Exception("null sql string"); 
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);
     
        // If the config result indicates that tracing is not enabled at the server side,
        // then set traceExists to false.
        SetConfigResult configResult = job.SetTraceLevel(level);
        if (configResult.JtOpenTraceLevel?.ToUpper() == "OFF") {
            traceExists = false;
        }

        Query query = job.Query(sql);

            try {
                query.Execute(1);
            } catch (Exception ex) {
                 String expectedMessage = "[SQL0104] Token ."+counter+" was not valid. Valid tokens: FOR USE SKIP WAIT WITH FETCH LIMIT ORDER UNION EXCEPT.";
                Assert.AreEqual(expectedMessage, ex.Message);
            }
            query.Close(); 
        

        GetTraceDataResult result = job.GetTraceDataResult();

        job.SetTraceLevel(ServerTraceLevel.OFF);
        
        job.Close();
        
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Id);
        bool hasExceptionInTrace = result.TraceData
                    .IndexOf("com.ibm.as400.access.AS400JDBCSQLSyntaxErrorException: [SQL0104] Token ." + counter) >= 0;
        if (traceExists) {
            if (!hasExceptionInTrace) { 
                Console.WriteLine("Trace does not have exception"); 
                Console.WriteLine(result.TraceData); 
            }
            Assert.IsTrue(hasExceptionInTrace);
        } else {
            if (hasExceptionInTrace) { 
                Console.WriteLine("Trace has exception"); 
                Console.WriteLine(result.TraceData); 
            }
            Assert.IsFalse(hasExceptionInTrace);
        }
        /* Make sure the server is reset */ 
        DaemonServer daemonServer2 = MapepireTest.GetTestDaemonServer(); 
        SqlJob job2 = new(); 
		ConnectionResult? cr2 = job2.Connect(daemonServer);
        
        job2.SetTraceLevel(ServerTraceLevel.OFF);

        job2.Close();
        }
    }
}
