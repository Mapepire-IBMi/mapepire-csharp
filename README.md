# mapepire-csharp
Mapepire client SDK for C#

## Overview

`mapepire-csharp` is a C# / .Net Core client SDK that leverages the [`mapepire-server`](https://github.com/Mapepire-IBMi/mapepire-server) to provide a new and convenient way to access Db2 on IBM i.


## Setup

### Requirements

- The SDK and interactive client require a supported .NET 10 runtime. Install the .NET 10 SDK to build the repository or run the tests.
- Mapepire follows [Microsoft's .NET support lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) for its supported runtime versions. .NET 10 LTS is the current baseline.

### Server Component Setup

In order for applications to use Db2 for i through this C# client SDK, the `mapepire-server` daemon must be installed and started-up on each IBM i. Follow the instructions [here](https://mapepire-ibmi.github.io/guides/sysadmin/) to learn about the installation and startup process of the server component.

## Example Usage

The InteractiveClient.cs program provides an example of using the SDK.  The connect method initializes a `DaemonServer` object that will be used to connect with the Server Component. A single `SqlJob` object is created to facilitate this connection from the client side. The query method creates a `Query` object and initializes with the specified query.  The query is executed and the results are displayed using the ToString method on each returned value.  

Note:  The C# SDK uses the JSON serialization facilities provided by System.Text.Json.  Consequently, the data returned by QueryResult.data is of type  List<Dictionary<String,Object>>, where `List` is the list of rows and `Dictionary` maps between column names and column values.  The type of the column value object is System.Text.Json.JsonElement. The string value may be obtained by using the ToString() method on the column value object. 


