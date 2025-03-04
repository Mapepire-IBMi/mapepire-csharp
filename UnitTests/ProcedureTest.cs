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
public sealed class  ProcedureTest

{
       private static String TestSchema = "mapepire_test";
    [TestInitialize]
    public void TestInitialize()
    {
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        
        string successString  = "connection created using (" + MapepireTest.host + "," + MapepireTest.port + "," + MapepireTest.user + ",*******)";
		SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);
        
        Query query = job.Query("CREATE SCHEMA " + TestSchema);
        try {
            query.Execute();
        } catch (Exception e) {
        } finally {
            query.Close();
            job.Close();
        }
      
    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    
    

    [TestMethod]
    public void NumberParameters()  {
        
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        string successString  = "connection created using (" + MapepireTest.host + "," + MapepireTest.port + "," + MapepireTest.user + ",*******)";
		SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);
       
        String testProc = "CREATE OR REPLACE PROCEDURE " + TestSchema + ".PROCEDURE_TEST("
                        + "  IN P1 INTEGER,"
                        + "  INOUT P2 INTEGER,"
                        + "  OUT P3 INTEGER"
                        + ")"
                        + "BEGIN"
                        + "  SET P3 = P1 + P2;"
                        + "  SET P2 = 0;"
                        + "END";
        Query queryA = job.Query(testProc);
        queryA.Execute();
        queryA.Close();
            
        QueryOptions options = new QueryOptions(false, false, [6, 4, 0]);
        Query queryB = job.Query("CALL " + TestSchema + ".PROCEDURE_TEST(?, ?, ?)", options);
        QueryResult result = queryB.Execute();
        queryB.Close();

        job.Close();
        Assert.IsNotNull(result.Metadata); 
        if (result.Metadata != null) { 
           Assert.IsNotNull(result.Metadata.Parameters);
           Assert.AreEqual(3, result.Metadata.Parameters.Count);
           Assert.AreEqual("P1",result.Metadata.Parameters.ElementAt(0).Name); 
           Assert.AreEqual("P2",result.Metadata.Parameters.ElementAt(1).Name); 
           Assert.AreEqual("P3",result.Metadata.Parameters.ElementAt(2).Name); 
           Assert.AreEqual("INTEGER",result.Metadata.Parameters.ElementAt(0).Type); 
           Assert.AreEqual("INTEGER",result.Metadata.Parameters.ElementAt(1).Type); 
           Assert.AreEqual("INTEGER",result.Metadata.Parameters.ElementAt(2).Type); 
        }
    
        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.HasResults);
        Assert.AreEqual(3, result.ParameterCount);
        Assert.AreEqual(0, result.UpdateCount);
        Assert.IsNotNull(result.Data); 
        if (result.Data != null) { 
           Assert.AreEqual(0, result.Data.Count);
        }

        Assert.IsNotNull(result.OutputParms);
       
        Assert.AreEqual(3, result.OutputParms.Count);


         Assert.AreEqual("P1",result.OutputParms.ElementAt(0).Name); 
           Assert.AreEqual("P2",result.OutputParms.ElementAt(1).Name); 
           Assert.AreEqual("P3",result.OutputParms.ElementAt(2).Name); 
           Assert.AreEqual("INTEGER",result.OutputParms.ElementAt(0).Type); 
           Assert.AreEqual("INTEGER",result.OutputParms.ElementAt(1).Type); 
           Assert.AreEqual("INTEGER",result.OutputParms.ElementAt(2).Type); 
           Assert.IsNull(result.OutputParms.ElementAt(0).Value); 
           Assert.AreEqual("0",result.OutputParms.ElementAt(1).Value.ToString()); 
           Assert.AreEqual("10",result.OutputParms.ElementAt(2).Value.ToString()); 
    }


    [TestMethod]
    public void CharParameters()  {
        
      DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        string successString  = "connection created using (" + MapepireTest.host + "," + MapepireTest.port + "," + MapepireTest.user + ",*******)";
		SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);

        String testProc = 
                "CREATE OR REPLACE PROCEDURE " + TestSchema + ".PROCEDURE_TEST_CHAR("
                        + "  IN P1 CHAR(5),"
                        + "  INOUT P2 CHAR(6),"
                        + "  OUT P3 CHAR(7)"
                        + ")"
                        + "BEGIN"
                        + "  SET P3 = RTRIM(P1) concat RTRIM(P2);"
                        + "  SET P2 = '';"
                        + "END";
        Query queryA = job.Query(testProc);
        queryA.Execute();
        queryA.Close();
    
        QueryOptions options = new QueryOptions(false, false, ["a", "b", ""]);
        Query queryB = job.Query("CALL " + TestSchema + ".PROCEDURE_TEST_CHAR(?, ?, ?)", options);
        QueryResult result = queryB.Execute();
        queryB.Close(); 

        job.Close();
        Assert.IsNotNull(result.Metadata);   
        if (result.Metadata != null) { 
            Assert.IsNotNull(result.Metadata.Parameters);
           Assert.AreEqual(3, result.Metadata.Parameters.Count);
           Assert.AreEqual("P1",result.Metadata.Parameters.ElementAt(0).Name); 
           Assert.AreEqual("P2",result.Metadata.Parameters.ElementAt(1).Name); 
           Assert.AreEqual("P3",result.Metadata.Parameters.ElementAt(2).Name); 
           Assert.AreEqual("CHAR",result.Metadata.Parameters.ElementAt(0).Type); 
           Assert.AreEqual("CHAR",result.Metadata.Parameters.ElementAt(1).Type); 
           Assert.AreEqual("CHAR",result.Metadata.Parameters.ElementAt(2).Type); 
           Assert.AreEqual(5,result.Metadata.Parameters.ElementAt(0).Precision); 
           Assert.AreEqual(6,result.Metadata.Parameters.ElementAt(1).Precision); 
           Assert.AreEqual(7,result.Metadata.Parameters.ElementAt(2).Precision); 

        }
        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.HasResults);
        Assert.AreEqual(3, result.ParameterCount);
        Assert.AreEqual(0, result.UpdateCount);
        Assert.IsNotNull(result.Data); 
        Assert.AreEqual(0, result.Data.Count);

        Assert.IsNotNull(result.OutputParms);
        Assert.AreEqual(3, result.OutputParms.Count);
        
         Assert.AreEqual("P1",result.OutputParms.ElementAt(0).Name); 
           Assert.AreEqual("P2",result.OutputParms.ElementAt(1).Name); 
           Assert.AreEqual("P3",result.OutputParms.ElementAt(2).Name); 
           Assert.AreEqual("CHAR",result.OutputParms.ElementAt(0).Type); 
           Assert.AreEqual("CHAR",result.OutputParms.ElementAt(1).Type); 
           Assert.AreEqual("CHAR",result.OutputParms.ElementAt(2).Type); 
           Assert.IsNull(result.OutputParms.ElementAt(0).Value); 
           Assert.AreEqual("",result.OutputParms.ElementAt(1).Value.ToString()); 
           Assert.AreEqual("ab", result.OutputParms.ElementAt(2).Value.ToString()); 
                  Assert.AreEqual(5, result.OutputParms.ElementAt(0).Precision); 
                  Assert.AreEqual(6, result.OutputParms.ElementAt(1).Precision); 
                  Assert.AreEqual(7,result.OutputParms.ElementAt(2).Precision); 
      
    }


    [TestMethod]
    public void VarcharParameters()  {
        
        DaemonServer daemonServer = MapepireTest.GetTestDaemonServer(); 
        string successString  = "connection created using (" + MapepireTest.host + "," + MapepireTest.port + "," + MapepireTest.user + ",*******)";
		SqlJob job = new(); 
		ConnectionResult? cr = job.Connect(daemonServer);
       

        String testProc =    "CREATE OR REPLACE PROCEDURE " + TestSchema + ".PROCEDURE_TEST_VARCHAR("
                        + "  IN P1 VARCHAR(5),"
                        + "  INOUT P2 VARCHAR(6),"
                        + "  OUT P3 VARCHAR(7)"
                        + ")"
                        + "BEGIN"
                        + "  SET P3 = P1 concat P2;"
                        + "  SET P2 = '';"
                        + "END";
        Query queryA = job.Query(testProc);
        queryA.Execute();
        queryA.Close();

        QueryOptions options = new QueryOptions(false, false, ["a", "b", "c"]);
        Query queryB = job.Query("CALL " + TestSchema + ".PROCEDURE_TEST_VARCHAR(?, ?, ?)", options);
        QueryResult result = queryB.Execute();
        queryB.Close();

        job.Close();
        Assert.IsNotNull(result.Metadata);
        Assert.IsNotNull(result.Metadata.Parameters);
        Assert.AreEqual(3, result.Metadata.Parameters.Count);

        

 if (result.Metadata != null) { 
            Assert.IsNotNull(result.Metadata.Parameters);
           Assert.AreEqual(3, result.Metadata.Parameters.Count);
           Assert.AreEqual("P1",result.Metadata.Parameters.ElementAt(0).Name); 
           Assert.AreEqual("P2",result.Metadata.Parameters.ElementAt(1).Name); 
           Assert.AreEqual("P3",result.Metadata.Parameters.ElementAt(2).Name); 
           Assert.AreEqual("VARCHAR",result.Metadata.Parameters.ElementAt(0).Type); 
           Assert.AreEqual("VARCHAR",result.Metadata.Parameters.ElementAt(1).Type); 
           Assert.AreEqual("VARCHAR",result.Metadata.Parameters.ElementAt(2).Type); 
           Assert.AreEqual(5,result.Metadata.Parameters.ElementAt(0).Precision); 
           Assert.AreEqual(6,result.Metadata.Parameters.ElementAt(1).Precision); 
           Assert.AreEqual(7,result.Metadata.Parameters.ElementAt(2).Precision); 

        }
        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.HasResults);
        Assert.AreEqual(3, result.ParameterCount);
        Assert.AreEqual(0, result.UpdateCount);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(0, result.Data.Count);

        Assert.IsNotNull(result.OutputParms);
        Assert.AreEqual(3, result.OutputParms.Count);

        Assert.AreEqual("P1",result.OutputParms.ElementAt(0).Name); 
           Assert.AreEqual("P2",result.OutputParms.ElementAt(1).Name); 
           Assert.AreEqual("P3",result.OutputParms.ElementAt(2).Name); 
           Assert.AreEqual("VARCHAR",result.OutputParms.ElementAt(0).Type); 
           Assert.AreEqual("VARCHAR",result.OutputParms.ElementAt(1).Type); 
           Assert.AreEqual("VARCHAR",result.OutputParms.ElementAt(2).Type); 
           Assert.IsNull(result.OutputParms.ElementAt(0).Value); 
           Assert.IsNotNull(result.OutputParms.ElementAt(1).Value); 
           Assert.IsNotNull(result.OutputParms.ElementAt(2).Value); 
           Assert.AreEqual("",result.OutputParms.ElementAt(1).Value.ToString()); 
           Assert.AreEqual("ab", result.OutputParms.ElementAt(2).Value.ToString()); 
                  Assert.AreEqual(5, result.OutputParms.ElementAt(0).Precision); 
                  Assert.AreEqual(6, result.OutputParms.ElementAt(1).Precision); 
                  Assert.AreEqual(7,result.OutputParms.ElementAt(2).Precision); 
      
    }

   
}