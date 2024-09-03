# Mapepire client SDK for C#
This project will provide a .NET WebSocket client class for the IBM i Mapepire WebSocket Data Server component.    
 

There will most likely be a version of the C# class that closely mirrors the following reference structure, but it's not there yet:   
https://mapepire-ibmi.github.io/reference/maintenance/referencearchitecture   
 

An initial prototype version of a C# Mapepire WebSocket Client class was created by Richard Schoen - MobiGoGo LLC and is named: ```MobiMapepireClient``` in file ```MobiMapepireClient.cs```. The class can be included in any current .NET project.   
 

❗The prototype class has only been tested with .Net 8.0 so far on Windows 10 and Windows 11. The older .NET Frameworks or .NET Standard have not been tested.   


# Valid SSL/TLS and Non SSL/TLS Scenarios
❗ Currently there is an issue with DotNet WebSockets, Windows 10 and SSL/TLS 1.3. The Mapepire server supports TLS1.3 by default so this .NET class will only work on Windows 11 or other platforms that support TLS1.3 natively unless you disable SSL or create a class version that uses an alternate WebSocket component.     
- On Windows 11 the class is fully usable using the base .NET ClientWebSocket class with or without valid certificates and SSL/TLS1.3.   
- On Windows 11 the class is fully usable using the base .NET ClientWebSocket class if SSL is disabled on the Mapepire Server. (For internal testing only)
- On Windows 10 the class is fully usable using the base .NET ClientWebSocket class if SSL is disabled on the Mapepire Server. (For internal testing only)  
- On Windows 10 the class is fully usable with SSL if it's re-worked to use the Rebex WebSocket controls instead of using the base .NET ClientWebSocket. Rebex supports TLS1.3 on Windows 10, Windows 11 and other platforms.

# Not Supported or Unkknown SSL/TLS Scenarios
- On Windows 10 the class is NOT usable using the base .NET ClientWebSocket class with or without valid certificates and SSL/TLS1.3. (Windows 10 does not support TLS1.3 without 3rd party controls such as Rebex.)
- It's currently unknown as to whether the .NET ClientWebSocket will work with .Net 8 on MacOS.
- It's currently unknown as to whether the .NET ClientWebSocket will work with .Net 8 on Linux.


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

# Sample C# test sequence using the prototype MobiMapepireClient.cs class
This is a very simple sample connect and query sequence.   

Copy the following statements into a .NET C# Console project. 
```
// Set user connection variables
bool secure = false; // This assumes non-SSL server
string host = "hostname";
int port = 8076;
string user = "user1";
string pass = "pass1";
bool allowinvalidcerts = false; // Set to true to allow invalid TLS1.3 certs on Windows 11.
                                // Windows 10 and below does not natively support TLS1.3

// Instantiate MobiMapepire Client class
var client = new MobiMapepireClient.Client();

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
