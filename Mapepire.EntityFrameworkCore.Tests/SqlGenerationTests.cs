using Microsoft.EntityFrameworkCore;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>
/// Offline tests: they check the SQL the provider generates and never connect to a server.
/// </summary>
[TestClass]
public sealed class SqlGenerationTests
{
    private static string Sql(Func<BloggingContext, IQueryable> query)
    {
        using var context = BloggingContext.CreateOffline();
        var sql = query(context).ToQueryString();
        Console.WriteLine(sql);
        return sql;
    }

    [TestMethod]
    public void Boolean_column_in_where_becomes_comparison()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.IsActive));
        StringAssert.Contains(sql, "WHERE \"b\".\"IsActive\" = 1");
    }

    [TestMethod]
    public void Negated_boolean_column_in_where()
    {
        var sql = Sql(c => c.Blogs.Where(b => !b.IsActive));
        StringAssert.Matches(sql, new System.Text.RegularExpressions.Regex("\"b\"\\.\"IsActive\" (= 0|<> 1)|NOT \\(\"b\"\\.\"IsActive\" = 1\\)"));
    }

    [TestMethod]
    public void Predicate_in_projection_becomes_case()
    {
        var sql = Sql(c => c.Blogs.Select(b => new { b.Id, Popular = b.Rating > 3 }));
        StringAssert.Contains(sql, "CASE");
        StringAssert.Contains(sql, "THEN 1");
        StringAssert.Contains(sql, "ELSE 0");
    }

    [TestMethod]
    public void Skip_and_take_are_inlined_as_offset_fetch()
    {
        var skip = 10;
        var take = 5;
        var sql = Sql(c => c.Blogs.OrderBy(b => b.Id).Skip(skip).Take(take));
        StringAssert.Contains(sql, "OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY");
    }

    [TestMethod]
    public void Take_only_uses_fetch_first()
    {
        var sql = Sql(c => c.Blogs.OrderBy(b => b.Id).Take(3));
        StringAssert.Contains(sql, "FETCH FIRST 3 ROWS ONLY");
    }

    [TestMethod]
    public void Take_zero_returns_no_rows_without_fetch_clause()
    {
        var take = 0;
        var sql = Sql(c => c.Blogs.OrderBy(b => b.Id).Take(take));
        Assert.IsFalse(sql.Contains("FETCH", StringComparison.Ordinal), sql);
        StringAssert.Contains(sql, "WHERE 0 = 1");
    }

    [TestMethod]
    public void Parameters_are_cast_to_their_store_type()
    {
        var name = "abc";
        var sql = Sql(c => c.Blogs.Where(b => b.Name == name));
        StringAssert.Contains(sql, "CAST(@");
        StringAssert.Contains(sql, " AS NVARCHAR(100))");
    }

    [TestMethod]
    public void Subquery_any_in_projection_is_valid()
    {
        var sql = Sql(c => c.Blogs.Select(b => new { b.Id, HasPosts = b.Posts.Any() }));
        // Db2 for i rejects EXISTS directly inside CASE WHEN (SQL0104).
        StringAssert.Contains(sql, "WHEN (SELECT 1 FROM SYSIBM.SYSDUMMY1 WHERE EXISTS (");
        StringAssert.Contains(sql, ") = 1 THEN 1");
    }

    [TestMethod]
    public void String_concatenation_uses_double_pipe()
    {
        var sql = Sql(c => c.Blogs.Select(b => b.Name + "!"));
        StringAssert.Contains(sql, "||");
    }

    [TestMethod]
    public void Modulo_uses_mod_function()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.Id % 2 == 0));
        StringAssert.Contains(sql, "MOD(\"b\".\"Id\", 2)");
    }

    [TestMethod]
    public void Contains_with_constant_uses_escaped_like()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.Name.Contains("50%")));
        StringAssert.Contains(sql, "LIKE '%50\\%%' ESCAPE '\\'");
    }

    [TestMethod]
    public void Contains_with_parameter_uses_locate()
    {
        var term = "x";
        var sql = Sql(c => c.Blogs.Where(b => b.Name.Contains(term)));
        StringAssert.Contains(sql, "LOCATE(");
    }

    [TestMethod]
    public void StartsWith_with_parameter_uses_left()
    {
        var prefix = "x";
        var sql = Sql(c => c.Blogs.Where(b => b.Name.StartsWith(prefix)));
        StringAssert.Contains(sql, "LEFT(");
    }

    [TestMethod]
    public void String_functions_are_translated()
    {
        var sql = Sql(c => c.Blogs.Select(b => new
        {
            Upper = b.Name.ToUpper(),
            Lower = b.Name.ToLower(),
            Trimmed = b.Name.Trim(),
            Sub = b.Name.Substring(1, 2),
            Len = b.Name.Length,
            Index = b.Name.IndexOf("a"),
        }));
        StringAssert.Contains(sql, "UPPER(");
        StringAssert.Contains(sql, "LOWER(");
        StringAssert.Contains(sql, "TRIM(");
        StringAssert.Contains(sql, "SUBSTR(\"b\".\"Name\", 1 + 1, 2)");
        StringAssert.Contains(sql, "LENGTH(");
        StringAssert.Contains(sql, "LOCATE(");
    }

    [TestMethod]
    public void Date_parts_are_translated()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.CreatedAt.Year == 2024 && b.CreatedAt < DateTime.Now));
        StringAssert.Contains(sql, "YEAR(\"b\".\"CreatedAt\")");
        StringAssert.Contains(sql, "CURRENT TIMESTAMP");
    }

    [TestMethod]
    public void DateTime_literal_uses_db2_timestamp_format()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.CreatedAt > new DateTime(2024, 1, 2, 3, 4, 5)));
        StringAssert.Contains(sql, "CAST('2024-01-02-03.04.05.000000' AS TIMESTAMP)");
    }

    [TestMethod]
    public void Contains_on_local_list_uses_in()
    {
        var ids = new List<int> { 1, 2, 3 };
        var sql = Sql(c => c.Blogs.Where(b => ids.Contains(b.Id)));
        StringAssert.Contains(sql, "\"b\".\"Id\" IN (");
        Assert.IsFalse(sql.Contains("json", StringComparison.OrdinalIgnoreCase), sql);
    }

    [TestMethod]
    public void Include_generates_left_join()
    {
        var sql = Sql(c => c.Blogs.Include(b => b.Posts));
        StringAssert.Contains(sql, "LEFT JOIN \"Posts\"");
    }
}
