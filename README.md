# mapepire-csharp
Mapepire client SDK for C#

## Overview

`mapepire-csharp` is a C# / .Net Core client SDK that leverages the [`mapepire-server`](https://github.com/Mapepire-IBMi/mapepire-server) to provide a new and convenient way to access Db2 on IBM i.


## Setup

### Requirements

### Server Component Setup

In order for applications to use Db2 for i through this C# client SDK, the `mapepire-server` daemon must be installed and started-up on each IBM i. Follow the instructions [here](https://mapepire-ibmi.github.io/guides/sysadmin/) to learn about the installation and startup process of the server component.

## Example Usage

The InteractiveClient.cs program provides an example of using the SDK.  The connect method initializes a `DaemonServer` object that will be used to connect with the Server Component. A single `SqlJob` object is created to facilitate this connection from the client side. The query method creates a `Query` object and initializes with the specified query.  The query is executed and the results are displayed using the ToString method on each returned value.  

Note:  The C# SDK uses the JSON serialization facilities provided by System.Text.Json.  Consequently, the data returned by QueryResult.data is of type  List<Dictionary<String,Object>>, where `List` is the list of rows and `Dictionary` maps between column names and column values.  The type of the column value object is System.Text.Json.JsonElement. The string value may be obtained by using the ToString() method on the column value object. 



## Entity Framework Core provider

The repository also contains an ADO.NET provider and an Entity Framework Core (EF Core 9) provider built on the SDK:

| Project | Description |
| --- | --- |
| `Mapepire.Data` | ADO.NET provider: `MapepireConnection`, `MapepireCommand`, `MapepireDataReader`, `MapepireTransaction`, `MapepireFactory`. |
| `Mapepire.EntityFrameworkCore` | EF Core provider. Configure it with `UseMapepire(...)`. |
| `Mapepire.EntityFrameworkCore.Tests` | Offline tests (SQL generation, plus connection and migration-lock behavior against the Mapepire mock server), and live tests that run when a Mapepire server is configured. |

### Usage

```csharp
public class BloggingContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseMapepire("Server=myibmi;User ID=MYUSER;Password=secret;Default Schema=MYLIB;Auto Commit=false");
}

// or, with dependency injection
services.AddMapepire<BloggingContext>(connectionString);
```

The ADO.NET layer can also be used on its own:

```csharp
using var connection = new MapepireConnection(connectionString);
connection.Open();
using var command = connection.CreateCommand();
command.CommandText = "SELECT NAME FROM MYLIB.CUSTOMERS WHERE ID = @id";
command.Parameters.AddWithValue("@id", 42);
var name = (string?)command.ExecuteScalar();
```

Parameters can be named (`@id`) or positional (`?`). Named markers are rewritten to positional markers before the statement is sent.

### Connection string keywords

| Keyword (synonyms) | Default | Description |
| --- | --- | --- |
| `Server` (`Host`, `Data Source`) | | Host name of the IBM i running `mapepire-server`. |
| `Port` | `8076` | Mapepire server port. |
| `User ID` (`UID`, `User`) / `Password` (`PWD`) | | IBM i user profile and password. |
| `Default Schema` (`Database`, `Schema`) | user profile | Library used for unqualified names. EF Core's `EnsureCreated`/`EnsureDeleted` create/drop this schema. |
| `Libraries` | | Extra libraries for the library list (comma separated). |
| `Auto Commit` | `true` | See *Transactions* below. |
| `Transaction Isolation` | server default | JDBC isolation: `none`, `read uncommitted`, `read committed`, `repeatable read`, `serializable`. |
| `Reject Unauthorized` / `Trust Server Certificate` | `true` / `false` | TLS certificate validation. |
| `CA` | | Base64 CA certificate used to validate the server certificate. |
| `Naming` | `sql` | JDBC naming. EF Core requires `sql`. |
| `Date Format` / `Time Format` | `iso` | JDBC date/time formats. |
| `Fetch Size` | `100` | Rows fetched per round trip. |
| `Pooling` / `Max Pool Size` | `true` / `100` | Server connections (websocket plus server job) are pooled per connection string. |
| `Idle Validation Seconds` | `30` | A pooled connection idle for longer than this is checked with a round trip before it is reused. |

### Transactions

* `Auto Commit=false`: real transactions. `BeginTransaction()` starts a unit of work that ends with COMMIT or ROLLBACK, and EF Core's `SaveChanges` is atomic. Outside an explicit transaction, each command is committed when it completes. **The tables involved must be journaled**; tables created in a schema made with `CREATE SCHEMA` are journaled automatically.
* `Auto Commit=true` (default, the same as JDBC): every statement is committed as it runs, so this also works on libraries that are not journaled. `BeginTransaction()` still succeeds so that EF Core works, but `Commit()` does nothing and `Rollback()` throws. A failing `SaveChanges` can leave earlier rows saved.

### Mapping and SQL notes

* Integer primary keys become `GENERATED BY DEFAULT AS IDENTITY`. Generated values are read back with `SELECT ... FROM FINAL TABLE (INSERT ...)`.
* `bool` maps to `SMALLINT` (0/1) so it works on every release. Use `HasColumnType("BOOLEAN")` on IBM i 7.5 and later.
* Strings map to `NVARCHAR(n)`, or `VARCHAR(n)` with `IsUnicode(false)`. Strings without a maximum length become `NVARCHAR(4000)` (`NVARCHAR(450)` for keys and indexes). Db2 for i limits a row to 32,766 bytes, so set `MaxLength` on wide tables.
* `Guid` maps to `CHAR(36)`, `DateTime` to `TIMESTAMP`, `DateOnly` to `DATE`, `TimeOnly`/`TimeSpan` to `TIME`, and `byte[]` to `VARBINARY(n)`.
* Optimistic concurrency (`[ConcurrencyCheck]`) works: the affected-row count of each UPDATE and DELETE is checked. Values the database generates during an UPDATE are not read back, and row-version (`[Timestamp]`) columns are not supported.
* Commands are sent one at a time; there is no statement batching.
* Translated methods and members:
  * strings: `ToUpper`, `ToLower`, `Trim`, `TrimStart`, `TrimEnd`, `Substring`, `Replace`, `IndexOf`, `Contains`, `StartsWith`, `EndsWith`, `Length`, `string.IsNullOrWhiteSpace`
  * `Math` functions
  * `DateTime`: `Now`, `UtcNow`, `Today`, `Date`, and the date parts (`Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `DayOfYear`)
* Not supported: computed columns, renaming sequences, moving a table to another schema. `RenameColumn` emits `ALTER TABLE ... RENAME COLUMN`, which needs an IBM i release that supports it.

### Running the tests

```
dotnet test Mapepire.EntityFrameworkCore.Tests
```

Offline tests always run. Connection and migration-lock tests run against the [Mapepire mock server](https://github.com/Mapepire-IBMi/mapepire-mockserver), which the build downloads and checks against its SHA-256 digest; they need Java 8 or later under `JAVA_HOME` or on the `PATH`. Live tests use `UnitTests/unitTestSettings.json`, which the `MAPEPIRE_HOST`, `MAPEPIRE_PORT`, `MAPEPIRE_USER` and `MAPEPIRE_PASSWORD` environment variables override. Each run creates a temporary `MPEFxxxxxx` schema and drops it afterwards. If no server can be reached, the live tests are reported as skipped (inconclusive).
