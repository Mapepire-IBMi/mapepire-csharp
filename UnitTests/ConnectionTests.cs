using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.VisualBasic;
using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace UnitTests;



[TestClass]
public sealed class  ConnectionTests

{
    private IConfigurationRoot? config;

    [TestInitialize]
    public void TestInitialize()
    {
        
      
    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    
    [TestMethod]
    public void TestSimpleConnect()
    {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        
        string successString  = "connection created using (" + MapepireTest.host + "," + MapepireTest.port + "," + MapepireTest.user + ",*******)";
		SqlJob newJob = new(); 
		ConnectionResult? cr = newJob.Connect(daemonServer);
        string? job= newJob.Id ?? throw new Exception("job is null");
        Console.WriteLine(successString);
        Console.WriteLine("JOB ="+job);
        VersionCheckResult result = newJob.GetVersion(); 
        Console.WriteLine("Version="+result.Version+" BuildDate="+result.BuildDate);
        Assert.IsTrue(job.IndexOf("QZDASOINIT") > 0); 
    }


    [TestMethod]
    public void TestInvalidConnection()  {
            SqlJob job = new SqlJob();

            try {
                DaemonServer daemonServer = MapepireTest.GetInvalidTestDaemonServer(); 
                job.Connect(daemonServer);
                Assert.Fail("Able to establish bad connection"); 
            } catch (Exception ex) {
                job.Close();
                Assert.IsTrue(ex.ToString().Contains("The application server rejected the connection."));
               
            }
     
    }
    /// <summary>Checks that reconnecting leaves the client usable even if IBM i reuses its prestart job.</summary>
    [TestMethod]
    public void ReconnectKeepsLiveConnectionUsable()
    {
        DaemonServer server = MapepireTest.GetTestDaemonServer();
        var job = new SqlJob();
        try
        {
            ConnectionResult? first = job.Connect(server);
            Assert.IsNotNull(first);
            Assert.IsTrue(first.Success);
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.Job));

            ConnectionResult? second = job.Connect(server);
            Assert.IsNotNull(second);
            Assert.IsTrue(second.Success);
            Assert.IsFalse(string.IsNullOrWhiteSpace(second.Job));
            Assert.AreEqual(second.Job, job.Id);
            Assert.IsTrue(job.GetVersion().Success);
            Console.WriteLine($"IBM i job IDs before and after reconnect: {first.Job}, {second.Job}");
        }
        finally
        {
            job.Close();
        }
    }

    [TestMethod]
    public void TestTraceSettings() {
         DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        
        string successString  = "connection created using (" + MapepireTest.host + "," + MapepireTest.port + "," + MapepireTest.user + ",*******)";
		SqlJob newJob = new(); 
		ConnectionResult? cr = newJob.Connect(daemonServer);
        string? job= newJob.Id ?? throw new Exception("job is null");
        Console.WriteLine(successString);
        newJob.SetJtOpenTraceDest(ServerTraceDest.FILE);
        newJob.SetJtOpenTraceLevel(ServerTraceLevel.ON);
        newJob.SetTraceLevel(ServerTraceLevel.ERRORS);
        newJob.SetTraceDest(ServerTraceDest.FILE); 
        newJob.SetTraceConfig(ServerTraceDest.FILE,ServerTraceLevel.ON,ServerTraceDest.FILE,ServerTraceLevel.ON);

        GetTraceDataResult result = newJob.GetTraceDataResult();
        Console.WriteLine("Trace data is "+ result.TraceData);
        Console.WriteLine("JTOpen trace data is "+result.JtOpenTraceData); 
        
    }
    
}
