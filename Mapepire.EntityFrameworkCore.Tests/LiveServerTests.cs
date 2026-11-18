using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Mapepire.Data;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>
/// End-to-end tests against a real Mapepire server. They use the same settings as the UnitTests project
/// (unitTestSettings.json, overridable with MAPEPIRE_HOST / MAPEPIRE_PORT / MAPEPIRE_USER / MAPEPIRE_PASSWORD)
/// and are reported as inconclusive when no server is configured. Each run creates and drops its own schema.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class LiveServerTests
{
    // Written once by ClassInitialize before any test runs, then only read. The class is [DoNotParallelize],
    // and MSTest runs ClassInitialize to completion before starting the class's tests.
    private static string? s_baseConnectionString;
    private static string? s_schema;
    private static string? s_skipReason;

    /// <summary>
    /// Reads the server settings and creates a fresh schema for this run. Records why the tests are skipped
    /// when no server is configured or reachable.
    /// </summary>
    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        WriteProgress("initialize.begin");
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("unitTestSettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var host = config["MAPEPIRE_HOST"];
        if (string.IsNullOrWhiteSpace(host))
        {
            s_skipReason = "MAPEPIRE_HOST is not configured.";
            WriteProgress("initialize.skipped: no server configured");
            return;
        }

        var builder = new MapepireConnectionStringBuilder
        {
            Server = host,
            Port = int.TryParse(config["MAPEPIRE_PORT"], out var port) ? port : 8076,
            UserId = config["MAPEPIRE_USER"],
            Password = config["MAPEPIRE_PASSWORD"],
            RejectUnauthorized = false,
        };
        s_baseConnectionString = builder.ConnectionString;
        s_schema = "MPEF" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        try
        {
            using var context = CreateContext(autoCommit: false);
            WriteProgress("initialize.EnsureDeleted.begin");
            context.Database.EnsureDeleted();
            WriteProgress("initialize.EnsureDeleted.end");
            WriteProgress("initialize.EnsureCreated.begin");
            context.Database.EnsureCreated();
            WriteProgress("initialize.EnsureCreated.end");
        }
        catch (MapepireException ex) when (ex.InnerException is not null && ex.SqlState is null)
        {
            s_skipReason = "Mapepire server not reachable: " + ex.Message;
            WriteProgress("initialize.skipped: server not reachable");
        }

        WriteProgress("initialize.end");
    }

    /// <summary>Drops this run's schema and closes all pooled connections.</summary>
    [ClassCleanup]
    public static void ClassCleanup()
    {
        WriteProgress("cleanup.begin");
        if (s_skipReason is null && s_baseConnectionString is not null)
        {
            using var context = CreateContext(autoCommit: false);
            WriteProgress("cleanup.EnsureDeleted.begin");
            context.Database.EnsureDeleted();
            WriteProgress("cleanup.EnsureDeleted.end");
        }

        WriteProgress("cleanup.ClearAllPools.begin");
        MapepireConnection.ClearAllPools();
        WriteProgress("cleanup.ClearAllPools.end");
        WriteProgress("cleanup.end");
    }

    /// <summary>SaveChanges reads identity keys back through FINAL TABLE, and every mapped type survives a round trip.</summary>
    [TestMethod]
    public void InsertGeneratesKeysAndQueryRoundTripsValues()
    {
        var created = new DateTime(2024, 5, 6, 7, 8, 9).AddTicks(1234560);
        int blogId;
        using (var context = Context())
        {
            var blog = new Blog
            {
                Name = "Round trip",
                IsActive = true,
                Rating = 4.25m,
                CreatedAt = created,
                Posts =
                [
                    new Post { Title = "First", Content = "Hello 'world'", PublishedOn = new DateOnly(2024, 5, 6) },
                    new Post { Title = "Second" },
                ],
            };
            context.Add(blog);
            context.SaveChanges();
            Assert.IsTrue(blog.Id > 0);
            Assert.IsTrue(blog.Posts.All(p => p.Id > 0 && p.BlogId == blog.Id));
            blogId = blog.Id;
        }

        using (var context = Context())
        {
            var blog = context.Blogs.Include(b => b.Posts).Single(b => b.Id == blogId);
            Assert.AreEqual("Round trip", blog.Name);
            Assert.IsTrue(blog.IsActive);
            Assert.AreEqual(4.25m, blog.Rating);
            Assert.AreEqual(created, blog.CreatedAt);
            Assert.AreEqual(2, blog.Posts.Count);
            var first = blog.Posts.Single(p => p.Title == "First");
            Assert.AreEqual("Hello 'world'", first.Content);
            Assert.AreEqual(new DateOnly(2024, 5, 6), first.PublishedOn);
            Assert.IsNull(blog.Posts.Single(p => p.Title == "Second").PublishedOn);
        }
    }

    /// <summary>Boolean filters, paging, Any, Count and translated string functions run on the server and return the right rows.</summary>
    [TestMethod]
    public void QueriesWithPredicatesPagingAndFunctions()
    {
        using (var context = Context())
        {
            context.AddRange(
                new Blog { Name = "Query A", IsActive = true, CreatedAt = DateTime.Today },
                new Blog { Name = "Query B", IsActive = false, CreatedAt = DateTime.Today },
                new Blog { Name = "Query C", IsActive = true, CreatedAt = DateTime.Today });
            context.SaveChanges();
        }

        using (var context = Context())
        {
            var prefix = "Query";
            var active = context.Blogs.Where(b => b.Name.StartsWith(prefix) && b.IsActive).OrderBy(b => b.Name).Select(b => b.Name).ToList();
            CollectionAssert.AreEqual(new[] { "Query A", "Query C" }, active);

            var page = context.Blogs.Where(b => b.Name.StartsWith(prefix)).OrderBy(b => b.Name).Skip(1).Take(1).Select(b => b.Name).ToList();
            CollectionAssert.AreEqual(new[] { "Query B" }, page);

            Assert.IsTrue(context.Blogs.Any(b => b.Name == "Query B"));
            Assert.IsFalse(context.Blogs.Any(b => b.Name == "No such blog"));
            Assert.AreEqual(3, context.Blogs.Count(b => b.Name.Contains("Query")));

            var projected = context.Blogs.Where(b => b.Name == "Query B")
                .Select(b => new { Upper = b.Name.ToUpper(), Inactive = !b.IsActive, Len = b.Name.Length })
                .Single();
            Assert.AreEqual("QUERY B", projected.Upper);
            Assert.IsTrue(projected.Inactive);
            Assert.AreEqual(7, projected.Len);
        }
    }

    /// <summary>Updates and deletes affect the right row, and a concurrent change raises DbUpdateConcurrencyException.</summary>
    [TestMethod]
    public void UpdateDeleteAndConcurrencyCheck()
    {
        int postId;
        using (var context = Context())
        {
            var blog = new Blog { Name = "Concurrency", CreatedAt = DateTime.Now, Posts = [new Post { Title = "v1" }] };
            context.Add(blog);
            context.SaveChanges();
            postId = blog.Posts[0].Id;
        }

        using (var context = Context())
        {
            var post = context.Posts.Single(p => p.Id == postId);
            post.Title = "v2";
            post.Version++;
            context.SaveChanges();
        }

        using (var context = Context())
        {
            var stale = context.Posts.Single(p => p.Id == postId);

            using (var other = Context())
            {
                other.Database.ExecuteSql($"UPDATE \"Posts\" SET \"Version\" = \"Version\" + 1 WHERE \"Id\" = {postId}");
            }

            stale.Title = "v3";
            Assert.ThrowsException<DbUpdateConcurrencyException>(() => context.SaveChanges());
        }

        using (var context = Context())
        {
            var post = context.Posts.Single(p => p.Id == postId);
            Assert.AreEqual("v2", post.Title);
            context.Remove(post);
            context.SaveChanges();
            Assert.IsFalse(context.Posts.Any(p => p.Id == postId));
        }
    }

    /// <summary>With Auto Commit=false, a SaveChanges that fails part-way leaves none of its rows behind.</summary>
    [TestMethod]
    public void FailedSaveRollsBackWholeUnitOfWork()
    {
        using (var context = Context())
        {
            context.Add(new Blog { Name = "Unique", CreatedAt = DateTime.Now });
            context.SaveChanges();
        }

        using (var context = Context())
        {
            context.Add(new Blog { Name = "Rolled back", CreatedAt = DateTime.Now });
            context.Add(new Blog { Name = "Unique", CreatedAt = DateTime.Now }); // violates the unique index
            Assert.ThrowsException<DbUpdateException>(() => context.SaveChanges());
        }

        using (var context = Context())
        {
            Assert.IsFalse(context.Blogs.Any(b => b.Name == "Rolled back"));
        }
    }

    /// <summary>Client-generated Guid keys work, and multi-row saves succeed in auto-commit mode.</summary>
    [TestMethod]
    public void GuidKeysAndAutoCommitMode()
    {
        var id = Guid.NewGuid();
        using (var context = Context(autoCommit: true))
        {
            context.Add(new Tag { Id = id, Label = "auto" });
            context.Add(new Tag { Id = Guid.NewGuid(), Label = "commit" });
            context.SaveChanges();
        }

        using (var context = Context())
        {
            Assert.AreEqual("auto", context.Tags.Single(t => t.Id == id).Label);
        }
    }

    /// <summary>The ADO.NET reader fetches a result larger than the fetch size across several round trips, in order.</summary>
    [TestMethod]
    public void AdoNetReaderPagesThroughLargeResults()
    {
        if (s_skipReason is not null)
        {
            Assert.Inconclusive(s_skipReason);
        }

        WriteProgress("paging.begin");
        using (var connection = new MapepireConnection(s_baseConnectionString + ";Fetch Size=7"))
        {
            WriteProgress("paging.Open.begin");
            connection.Open();
            WriteProgress("paging.Open.end");
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "WITH N(I) AS (SELECT 1 FROM SYSIBM.SYSDUMMY1 UNION ALL SELECT I + 1 FROM N WHERE I < @max) SELECT I FROM N";
                command.Parameters.AddWithValue("@max", 50);
                WriteProgress("paging.ExecuteReader.begin");
                using (var reader = command.ExecuteReader())
                {
                    WriteProgress("paging.ExecuteReader.end");
                    try
                    {
                        var count = 0;
                        while (true)
                        {
                            WriteProgress($"paging.Read.begin: rows read={count}");
                            if (!reader.Read())
                            {
                                WriteProgress("paging.Read.end: end of results");
                                break;
                            }

                            count++;
                            WriteProgress($"paging.Read.end: rows read={count}");
                            Assert.AreEqual(count, reader.GetInt32(0));
                        }

                        Assert.AreEqual(50, count);
                    }
                    finally
                    {
                        WriteProgress("paging.reader.Dispose.begin");
                    }
                }

                WriteProgress("paging.reader.Dispose.end");
                WriteProgress("paging.command.Dispose.begin");
            }

            WriteProgress("paging.command.Dispose.end");
            WriteProgress("paging.connection.Dispose.begin");
        }

        WriteProgress("paging.connection.Dispose.end");
        WriteProgress("paging.end");
    }

    private static BloggingContext CreateContext(bool autoCommit)
    {
        var builder = new MapepireConnectionStringBuilder(s_baseConnectionString) { DefaultSchema = s_schema, AutoCommit = autoCommit };
        return BloggingContext.Create(builder.ConnectionString, message =>
        {
            Console.WriteLine(message);
            WriteProgress(message);
        });
    }

    /// <summary>
    /// Persists live-test progress outside MSTest's captured output so it survives a test-host timeout.
    /// Connection strings and parameter values must not be passed to this method.
    /// </summary>
    private static void WriteProgress(string message)
    {
        var path = Environment.GetEnvironmentVariable("MAPEPIRE_TEST_PROGRESS_FILE");
        if (!string.IsNullOrWhiteSpace(path))
        {
            // Each append closes the file; a killed test host must not lose buffered progress.
            File.AppendAllText(path, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
    }

    /// <summary>Creates a context on this run's schema, or marks the test inconclusive when there is no server.</summary>
    private static BloggingContext Context(bool autoCommit = false)
    {
        if (s_skipReason is not null)
        {
            Assert.Inconclusive(s_skipReason);
        }

        return CreateContext(autoCommit);
    }
}
