using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.ConnectionService;

namespace Tests;

[TestFixture]
public class EfSearchCollationTest
{
    private string _dbPath = null!;
    private EfCoreContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"swimdoc-search-{Guid.NewGuid():N}.db");
        var connectionService = new DatabaseConnectionService();
        connectionService.SetConnection($"Data Source={_dbPath}");

        _context = new EfCoreContext(
            new DbContextOptionsBuilder<EfCoreContext>().UseSwimDocSqlite().Options,
            connectionService);

        _context.Athletes.Add(new Athlete
        {
            FirstName = "Иван",
            LastName = "Петров",
            Gender = Gender.Male,
            YearOfBirth = 2010
        });
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [TestCase("петров", 1)]
    [TestCase("Петров", 1)]
    [TestCase("ПЕТРОВ", 1)]
    [TestCase("иван", 1)]
    [TestCase("ИВАН", 1)]
    public async Task ContainsIgnoreCase_FindsCyrillicRegardlessOfCase(string term, int expected)
    {
        var query = _context.Athletes.AsNoTracking().Where(a =>
            SwimDocDbFunctions.ContainsIgnoreCase(a.LastName, term) ||
            SwimDocDbFunctions.ContainsIgnoreCase(a.FirstName, term));

        TestContext.Out.WriteLine(query.ToQueryString());
        Assert.That(await query.CountAsync(), Is.EqualTo(expected));
    }
}
