using Mapepire.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
    private static string? _baseConnectionString;
    private static string? _schema;
    private static string? _skipReason;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("unitTestSettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var host = config["MAPEPIRE_HOST"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _skipReason = "MAPEPIRE_HOST is not configured.";
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
        _baseConnectionString = builder.ConnectionString;
        _schema = "MPEF" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        try
        {
            using var context = CreateContext(autoCommit: false);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
        catch (MapepireException ex) when (ex.InnerException is not null && ex.SqlState is null)
        {
            _skipReason = "Mapepire server not reachable: " + ex.Message;
        }
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (_skipReason == null && _baseConnectionString != null)
        {
            using var context = CreateContext(autoCommit: false);
            context.Database.EnsureDeleted();
        }

        MapepireConnection.ClearAllPools();
    }

    private static BloggingContext CreateContext(bool autoCommit)
    {
        var builder = new MapepireConnectionStringBuilder(_baseConnectionString) { DefaultSchema = _schema, AutoCommit = autoCommit };
        return BloggingContext.Create(builder.ConnectionString, Console.WriteLine);
    }

    private static BloggingContext Context(bool autoCommit = false)
    {
        if (_skipReason != null)
        {
            Assert.Inconclusive(_skipReason);
        }

        return CreateContext(autoCommit);
    }

    [TestMethod]
    public void Insert_generates_keys_and_query_round_trips_values()
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

    [TestMethod]
    public void Queries_with_predicates_paging_and_functions()
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

    [TestMethod]
    public void Update_delete_and_concurrency_check()
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

    [TestMethod]
    public void Failed_save_rolls_back_whole_unit_of_work()
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

    [TestMethod]
    public void Guid_keys_and_auto_commit_mode()
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

    [TestMethod]
    public void Ado_net_reader_pages_through_large_results()
    {
        if (_skipReason != null)
        {
            Assert.Inconclusive(_skipReason);
        }

        using var connection = new MapepireConnection(_baseConnectionString + ";Fetch Size=7");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "WITH N(I) AS (SELECT 1 FROM SYSIBM.SYSDUMMY1 UNION ALL SELECT I + 1 FROM N WHERE I < @max) SELECT I FROM N";
        command.Parameters.AddWithValue("@max", 50);
        using var reader = command.ExecuteReader();
        var count = 0;
        while (reader.Read())
        {
            count++;
            Assert.AreEqual(count, reader.GetInt32(0));
        }

        Assert.AreEqual(50, count);
    }
}
