using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>A blog: identity key, unique name, and a one-to-many relationship to posts.</summary>
public sealed class Blog
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public decimal? Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<Post> Posts { get; set; } = [];
}

/// <summary>A post: optional content and date, and a concurrency-checked version.</summary>
public sealed class Post
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    public DateOnly? PublishedOn { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; }

    public int BlogId { get; set; }

    // Required navigation: EF Core sets it when the post is loaded or attached to a blog.
    public Blog Blog { get; set; } = null!;
}

/// <summary>A tag with a client-generated <see cref="Guid"/> key.</summary>
public sealed class Tag
{
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string Label { get; set; } = string.Empty;
}

/// <summary>The test model used by the offline and live tests.</summary>
public sealed class BloggingContext(DbContextOptions<BloggingContext> options) : DbContext(options)
{
    public DbSet<Blog> Blogs => Set<Blog>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<Tag> Tags => Set<Tag>();

    /// <summary>Creates a context for a connection string.</summary>
    /// <param name="connectionString">The Mapepire connection string.</param>
    /// <param name="log">Receives each executed or failed SQL command, if set.</param>
    public static BloggingContext Create(string connectionString, Action<string>? log = null)
    {
        var builder = new DbContextOptionsBuilder<BloggingContext>().UseMapepire(connectionString);
        if (log is not null)
        {
            builder.LogTo(log, [Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted, Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError]);
        }

        return new(builder.Options);
    }

    /// <summary>A context that can generate SQL but is never opened.</summary>
    public static BloggingContext CreateOffline()
        => Create("Server=example.invalid;User ID=TEST;Password=unused;Default Schema=EFTEST");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>().HasIndex(b => b.Name).IsUnique();
        modelBuilder.Entity<Post>().HasIndex(p => p.PublishedOn);
    }
}
