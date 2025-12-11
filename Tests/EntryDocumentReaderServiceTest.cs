using System.Runtime.InteropServices;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using ServiceLayer.ConnectionService;
using ServiceLayer.EntryDocumentReaderService;

namespace Tests;

[TestFixture]
public class EntryDocumentReaderServiceTest
{
    private EfCoreContext _context;
    private EntryDocumentReaderService _service;
    private static readonly string ENTRY_DOCUMENT_TEMPLATE = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "EntryDocument", "ClubEntry.xlsx");

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        ExcelPackage.License.SetNonCommercialPersonal("Aliaksei Kryzhanouski");
    }

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<EfCoreContext>()
            .UseSqlite()
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, LogLevel.Information)
            .Options;
        var connectionService = new DatabaseConnectionService();
        connectionService.SetConnection(Path.Combine(Directory.GetCurrentDirectory(), "Test.sqlite"));
        _context = new EfCoreContext(options, connectionService);

        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _service = new EntryDocumentReaderService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Test]
    public void Test()
    {
        var result = _service.Read(ENTRY_DOCUMENT_TEMPLATE);

        Assert.That(result, Is.Not.Null);
    }
}