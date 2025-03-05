
using System;
using io.github.mapepire_ibmi;
using io.github.mapepire_ibmi.types;

namespace UnitTests;



[TestClass]
public sealed class  CLTests

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
    public void ValidCLCommand()
    {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        
        string successString  = "connection created using (" + MapepireTest.host + "," + MapepireTest.port + "," + MapepireTest.user + ",*******)";
		SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);
        
        Query query = job.ClCommand("WRKACTJOB");
        QueryResult result = query.Execute(); 
        job.Close();

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Data);
        Console.WriteLine(result.Data); 

    }

  
    [TestMethod]
    public void InvalidCLCommand()  {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        
        SqlJob job = new();
        ConnectionResult? cr = job.Connect(daemonServer);
        
        
        Query query = job.ClCommand("INVALIDCOMMAND");
        QueryResult result = query.Execute();
        job.Close();

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.IsDone);
        Assert.IsNotNull(result.Id);
        Assert.IsTrue(result.Data.Count > 0);
        Assert.AreEqual(-443, result.SqlRc);
        Assert.AreEqual("38501", result.SqlState);
        Assert.AreEqual("[CPF0006] Errors occurred in command.", result.Error);
    }
}




