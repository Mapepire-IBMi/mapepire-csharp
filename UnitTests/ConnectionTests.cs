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
        
        Console.WriteLine(Directory.GetCurrentDirectory());
        config = new ConfigurationBuilder()
            .AddJsonFile("unitTestSettings.json")
             .Build();


    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    
    [TestMethod]
    public void TestSimpleConnect()
    {
        Console.WriteLine("Running ConnectionTests.TestSimpleConnect");
        if (config == null) throw new Exception("config is null"); 
string? host = config["host"] ?? throw new Exception("host not specifed in unitTestSettings.json");
        string? portString = config["port"] ?? throw new Exception("port not specifed in unitTestSettings.json");
        int port = Int32.Parse(portString);
string? user = config["user"] ?? throw new Exception("user not specifed in unitTestSettings.json");
        string? password = config["password"] ?? throw new Exception("password not specifed in unitTestSettings.json");
        DaemonServer daemonServer = new(host, port, user, password, false);
        string successString  = "connection created using (" + host + "," + port + "," + user + ",*******)";
		SqlJob newJob = new(); 
		ConnectionResult? cr = newJob.connect(daemonServer);
        string? job= newJob.Id ?? throw new Exception("job is null");
        Console.WriteLine(successString);
        Console.WriteLine("JOB ="+job);

        Assert.IsTrue(job.IndexOf("QZDASOINIT") > 0); 
    }
}
