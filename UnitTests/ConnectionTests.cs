using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.VisualBasic;
using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi;

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
		ConnectionResult? cr = newJob.connect(daemonServer);
        string? job= newJob.Id ?? throw new Exception("job is null");
        Console.WriteLine(successString);
        Console.WriteLine("JOB ="+job);

        Assert.IsTrue(job.IndexOf("QZDASOINIT") > 0); 
    }
}
