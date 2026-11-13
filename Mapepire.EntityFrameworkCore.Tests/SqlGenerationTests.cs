using Microsoft.EntityFrameworkCore;

namespace Mapepire.EntityFrameworkCore.Tests;

/// <summary>
/// Offline tests: they check the SQL the provider generates and never connect to a server.
/// </summary>
[TestClass]
public sealed class SqlGenerationTests
{
    [TestMethod]
    public void BooleanColumnInWhereBecomesComparison()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.IsActive));
        StringAssert.Contains(sql, "WHERE \"b\".\"IsActive\" = 1");
    }

    [TestMethod]
    public void NegatedBooleanColumnInWhere()
    {
        var sql = Sql(c => c.Blogs.Where(b => !b.IsActive));
        StringAssert.Matches(sql, new System.Text.RegularExpressions.Regex("\"b\"\\.\"IsActive\" (= 0|<> 1)|NOT \\(\"b\"\\.\"IsActive\" = 1\\)"));
    }

    [TestMethod]
    public void PredicateInProjectionBecomesCase()
    {
        var sql = Sql(c => c.Blogs.Select(b => new { b.Id, Popular = b.Rating > 3 }));
        StringAssert.Contains(sql, "CASE");
        StringAssert.Contains(sql, "THEN 1");
        StringAssert.Contains(sql, "ELSE 0");
    }

    [TestMethod]
    public void SkipAndTakeAreInlinedAsOffsetFetch()
    {
        var skip = 10;
        var take = 5;
        var sql = Sql(c => c.Blogs.OrderBy(b => b.Id).Skip(skip).Take(take));
        StringAssert.Contains(sql, "OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY");
    }

    [TestMethod]
    public void TakeOnlyUsesFetchFirst()
    {
        var sql = Sql(c => c.Blogs.OrderBy(b => b.Id).Take(3));
        StringAssert.Contains(sql, "FETCH FIRST 3 ROWS ONLY");
    }

    [TestMethod]
    public void TakeZeroReturnsNoRowsWithoutFetchClause()
    {
        var take = 0;
        var sql = Sql(c => c.Blogs.OrderBy(b => b.Id).Take(take));
        Assert.IsFalse(sql.Contains("FETCH", StringComparison.Ordinal), sql);
        StringAssert.Contains(sql, "WHERE 0 = 1");
    }

    [TestMethod]
    public void ParametersAreCastToTheirStoreType()
    {
        var name = "abc";
        var sql = Sql(c => c.Blogs.Where(b => b.Name == name));
        StringAssert.Contains(sql, "CAST(@");
        StringAssert.Contains(sql, " AS NVARCHAR(100))");
    }

    [TestMethod]
    public void SubqueryAnyInProjectionIsValid()
    {
        var sql = Sql(c => c.Blogs.Select(b => new { b.Id, HasPosts = b.Posts.Any() }));
        // Db2 for i rejects EXISTS directly inside CASE WHEN (SQL0104).
        StringAssert.Contains(sql, "WHEN (SELECT 1 FROM SYSIBM.SYSDUMMY1 WHERE EXISTS (");
        StringAssert.Contains(sql, ") = 1 THEN 1");
    }

    [TestMethod]
    public void StringConcatenationUsesDoublePipe()
    {
        var sql = Sql(c => c.Blogs.Select(b => b.Name + "!"));
        StringAssert.Contains(sql, "||");
    }

    [TestMethod]
    public void ModuloUsesModFunction()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.Id % 2 == 0));
        StringAssert.Contains(sql, "MOD(\"b\".\"Id\", 2)");
    }

    [TestMethod]
    public void ContainsWithConstantUsesEscapedLike()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.Name.Contains("50%")));
        StringAssert.Contains(sql, "LIKE '%50\\%%' ESCAPE '\\'");
    }

    [TestMethod]
    public void ContainsWithParameterUsesLocate()
    {
        var term = "x";
        var sql = Sql(c => c.Blogs.Where(b => b.Name.Contains(term)));
        StringAssert.Contains(sql, "LOCATE(");
    }

    [TestMethod]
    public void StartsWithWithParameterUsesLeft()
    {
        var prefix = "x";
        var sql = Sql(c => c.Blogs.Where(b => b.Name.StartsWith(prefix)));
        StringAssert.Contains(sql, "LEFT(");
    }

    /// <summary>String methods and members translate to their Db2 for i functions.</summary>
    [TestMethod]
    public void StringFunctionsAreTranslated()
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
    public void DatePartsAreTranslated()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.CreatedAt.Year == 2024 && b.CreatedAt < DateTime.Now));
        StringAssert.Contains(sql, "YEAR(\"b\".\"CreatedAt\")");
        StringAssert.Contains(sql, "CURRENT TIMESTAMP");
    }

    [TestMethod]
    public void DateTimeLiteralUsesDb2TimestampFormat()
    {
        var sql = Sql(c => c.Blogs.Where(b => b.CreatedAt > new DateTime(2024, 1, 2, 3, 4, 5)));
        StringAssert.Contains(sql, "CAST('2024-01-02-03.04.05.000000' AS TIMESTAMP)");
    }

    [TestMethod]
    public void ContainsOnLocalListUsesIn()
    {
        var ids = new List<int> { 1, 2, 3 };
        var sql = Sql(c => c.Blogs.Where(b => ids.Contains(b.Id)));
        StringAssert.Contains(sql, "\"b\".\"Id\" IN (");
        Assert.IsFalse(sql.Contains("json", StringComparison.OrdinalIgnoreCase), sql);
    }

    [TestMethod]
    public void IncludeGeneratesLeftJoin()
    {
        var sql = Sql(c => c.Blogs.Include(b => b.Posts));
        StringAssert.Contains(sql, "LEFT JOIN \"Posts\"");
    }

    private static string Sql(Func<BloggingContext, IQueryable> query)
    {
        using var context = BloggingContext.CreateOffline();
        var sql = query(context).ToQueryString();
        Console.WriteLine(sql);
        return sql;
    }
}
