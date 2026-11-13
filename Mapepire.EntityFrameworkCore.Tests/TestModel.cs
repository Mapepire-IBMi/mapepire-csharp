using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Mapepire.EntityFrameworkCore.Tests;

public class Blog
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public decimal? Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<Post> Posts { get; set; } = [];
}

public class Post
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    public DateOnly? PublishedOn { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; }

    public int BlogId { get; set; }

    public Blog Blog { get; set; } = null!;
}

public class Tag
{
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string Label { get; set; } = string.Empty;
}

public class BloggingContext(DbContextOptions<BloggingContext> options) : DbContext(options)
{
    public DbSet<Blog> Blogs => Set<Blog>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>().HasIndex(b => b.Name).IsUnique();
        modelBuilder.Entity<Post>().HasIndex(p => p.PublishedOn);
    }

    public static BloggingContext Create(string connectionString)
        => new(new DbContextOptionsBuilder<BloggingContext>().UseMapepire(connectionString).Options);

    /// <summary>A context that can generate SQL but is never opened.</summary>
    public static BloggingContext CreateOffline()
        => Create("Server=example.invalid;User ID=TEST;Password=unused;Default Schema=EFTEST");
}
