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
        
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
 
        string successString  = "connection created using (" + MapepireTest.host + "," + MapepireTest.port + "," + MapepireTest.user + ",*******)";
		 sqlJob = new(); 
		ConnectionResult? cr = sqlJob.Connect(daemonServer);

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
        String Line="select job_name  as MYJOB, current timestamp AS CURTIME from sysibm.sysdummy1"; 
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
                jsonElement = (JsonElement?)  hashmap.GetValueOrDefault("CURTIME");
                String? curtime = jsonElement.ToString(); 
                if (job == null) throw new Exception("Null JOB"); 
                Console.WriteLine("..Line is "+job+","+curtime); 
			} else {
                Assert.Fail("Iterator was empty"); 
                return;
            }
			
		}
		// Close query 
		query.Close();

        Assert.IsTrue(job.IndexOf("QZDASOINIT") > 0); 
    }


    [TestMethod]
    public void TestBigQuery()
    {
        Console.WriteLine("Running QueryTests.TestBigQuery");
        
        if (sqlJob == null) throw new Exception("sqlJob is null"); 
        String? job; 
        String Line="select job_name  as MYJOB, A.* from QSYS2.SYSCOLUMNS A FETCH FIRST 1000 rows only"; 
        Query query = sqlJob.Query(Line);
		QueryResult result = query.Execute(1000);

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
		query.Close();

        Assert.IsTrue(job.IndexOf("QZDASOINIT") > 0); 
    }


}
