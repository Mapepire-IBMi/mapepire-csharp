# Mapepire client SDK for C#
This class is a .NET WebSocket client class for the IBM i Mapepire WebSocket Data Server component.    

The Mapepire server supports TLS1.3 by default so this class will only work on Windows 11 or other platforms that support TLS1.3 natively unless you disable SSL.  

❗ Disabling SSL is not advised. 

Learn more about the IBM i Mapepire Server component here:   
https://github.com/Mapepire-IBMi

❗ Note: Currently there is an issue with DotNet WebSockets, SSL and Windows 10 and TLS 1.3. 
- On Windows 11 the class is now fully usable with or without valid certificates and TLS1.3
- On Windows 10 the class is only usable if SSL/TLS is disabled.

# Starting up mapepire server without SSL for testing   
Start up the mapepire server without SSL via the following command:   
```MP_UNSECURE=true /QOpenSys/pkgs/bin/mapepire```

# Submit start up for mapepire server without SSL via QSHBASH   
```
SBMJOB CMD(QSHONI/QSHBASH CMDLINE('MP_UNSECURE=true /QOpenSys/pkgs/bin/mapepire') 
SETPKGPATH(*YES) PRTSTDOUT(*YES) PRTSPLF(STRMAPEPIR)
PASEJOBNAM(MAPEPIRETH)) JOB(STRMAPEPIR) JOBQ(QUSRNOMAX) USER(&USERID)
JOBMSGQFL(*WRAP) ALWMLTTHD(*YES)            
```

# Starting up mapepire server with SSL TLS 1.3 for testing   
Start up the mapepire server without SSL via the following command:   
```/QOpenSys/pkgs/bin/mapepire```

# Submit start up for mapepire server with SSL TLS 1.3 via QSHBASH   
```
SBMJOB CMD(QSHONI/QSHBASH CMDLINE('/QOpenSys/pkgs/bin/mapepire') 
SETPKGPATH(*YES) PRTSTDOUT(*YES) PRTSPLF(STRMAPEPIR)
PASEJOBNAM(MAPEPIRETH)) JOB(STRMAPEPIR) JOBQ(QUSRNOMAX) USER(&USERID)
JOBMSGQFL(*WRAP) ALWMLTTHD(*YES)            
```

# Sample C# test sequence   
This is a very simple sample connect and query sequence.   

Copy the following statements into a Dotnet C# Console project. 
```
// Set user connection variables
bool secure = false; // This assumes non-SSL server
string host = "hostname";
int port = 8076;
string user = "user1";
string pass = "pass1";
bool allowinvalidcerts = false; // Set to true to allow invalid TLS1.3 certs on Windows 11.
                                // Windows 10 and below does not natively support TLS1.3

// Instantiate Mapepire Client class
var client = new MapepireClient.Client();

// Connect to WebSocket server
var taskWebConnect = Task.Run(() => client.Connect(host,user,pass,port,secure));
taskWebConnect.Wait();

// Write out connection results
Console.WriteLine(client.GetConnectionResults());

// Run SQL Query
var taskWebQuery = Task.Run(() => client.ExecSqlQuery("select * from qiws.qcustcdt","q1"));
taskWebQuery.Wait();

// Write out sql query results
Console.WriteLine(client.GetQueryResults());

// Disconnect from WebSocket server
var taskWebDisconnect = Task.Run(() => client.Disconnect());
taskWebDisconnect.Wait();
```
