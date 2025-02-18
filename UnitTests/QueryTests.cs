using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.VisualBasic;
using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi;
using System.Text.Json;

namespace UnitTests;



[TestClass]
public sealed class  QueryTests

{
    private IConfigurationRoot? config;

    SqlJob? sqlJob ; 
    [TestInitialize]
    public void TestInitialize()
    {
        
        Console.WriteLine(Directory.GetCurrentDirectory());
        config = new ConfigurationBuilder()
            .AddJsonFile("unitTestSettings.json")
             .Build();

 if (config == null) throw new Exception("config is null"); 
string? host = config["host"] ?? throw new Exception("host not specifed in unitTestSettings.json");
        string? portString = config["port"] ?? throw new Exception("port not specifed in unitTestSettings.json");
        int port = Int32.Parse(portString);
string? user = config["user"] ?? throw new Exception("user not specifed in unitTestSettings.json");
        string? password = config["password"] ?? throw new Exception("password not specifed in unitTestSettings.json");
        DaemonServer daemonServer = new(host, port, user, password, false);
        string successString  = "connection created using (" + host + "," + port + "," + user + ",*******)";
		 sqlJob = new(); 
		ConnectionResult? cr = sqlJob.connect(daemonServer);

    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    
    [TestMethod]
    public void TestSimpleQuery()
    {
        Console.WriteLine("Running QueryTests.TestSimpleQuery");
        
        if (sqlJob == null) throw new Exception("sqlJob is null"); 
        String? job; 
        String Line="select job_name as MYJOB from sysibm.sysdummy1"; 
        Query query = sqlJob.Query(Line);
		QueryResult result = query.Execute();

		String? errorString = result.Error;
		if (errorString != null) { 
			Assert.Fail( "Query returned errorString:"+errorString); 
            return; 
		} else { 
			List<Dictionary<String,Object>>? data = result.Data ?? throw new Exception("NULL data");
            List<Dictionary<String,Object>>.Enumerator enumerator = data.GetEnumerator(); 
			if (enumerator.MoveNext()) { 
				Dictionary<String, Object> hashmap = (Dictionary <String,Object>) enumerator.Current;
                JsonElement? jsonElement = (JsonElement?)  hashmap.GetValueOrDefault("MYJOB");
                job = jsonElement.ToString();
                if (job == null) throw new Exception("Null JOB"); 
			} else {
                Assert.Fail("Iterator was empty"); 
                return;
            }
			
		}
		// Close query 
		query.close();

        Assert.IsTrue(job.IndexOf("QZDASOINIT") > 0); 
    }
}
