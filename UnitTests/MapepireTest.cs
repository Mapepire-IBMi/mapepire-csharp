using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.VisualBasic;
using io.github.mapepire_ibmi.types;
using io.github.mapepire_ibmi;

namespace UnitTests;




public sealed class  MapepireTest

{
    private static IConfigurationRoot? config = null;

    public static string? host; 
    public static int port; 

    public static string? user;
    public static string? password;

    public static DaemonServer GetTestDaemonServer()
    {
        
        if (config == null) { 
        config = new ConfigurationBuilder()
            .AddJsonFile("unitTestSettings.json")
            .AddEnvironmentVariables()
             .Build();
        }
        host = config["MAPEPIRE_HOST"] ?? throw new Exception("HOST not specifed in ENVVAR or  unitTestSettings.json");
        string? portString = config["MAPEPIRE_PORT"] ?? throw new Exception("PORT not specifed in ENVVARA or unitTestSettings.json");
        port = Int32.Parse(portString);
        user = config["MAPEPIRE_USER"] ?? throw new Exception("USER not specifed in ENVVAR or unitTestSettings.json");
        password = config["MAPEPIRE_PASSWORD"] ?? throw new Exception("password not specifed in ENVVAR or unitTestSettings.json");
        DaemonServer daemonServer = new(host, port, user, password, false);
        return daemonServer; 


    }

public static DaemonServer GetInvalidTestDaemonServer() {
     if (config == null) { 
        config = new ConfigurationBuilder()
            .AddJsonFile("unitTestSettings.json")
            .AddEnvironmentVariables()
             .Build();
        }
        host = config["MAPEPIRE_HOST"] ?? throw new Exception("HOST not specifed in ENVVAR or  unitTestSettings.json");
        string? portString = config["MAPEPIRE_PORT"] ?? throw new Exception("PORT not specifed in ENVVARA or unitTestSettings.json");
        port = Int32.Parse(portString);
        DaemonServer daemonServer = new(host, port, "BAD_USER", "BAD PASSWORD", false);
        return daemonServer;
}
 
}
