using DataLayer.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ServiceLayer.ConnectionService;

namespace Tests.TestInfrastructure;

public abstract class DatabaseTestFixture
{
    protected EfCoreContext Context { get; private set; } = null!;
    private string _dbPath = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        ExcelPackage.License.SetNonCommercialPersonal("Aliaksei Kryzhanouski");
    }

    [SetUp]
    public void SetUpDatabase()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"swimdoc-test-{Guid.NewGuid():N}.db");
        File.Create(_dbPath).Close();

        var connectionService = new DatabaseConnectionService();
        connectionService.SetConnection($"Data Source={_dbPath}");

        var options = new DbContextOptionsBuilder<EfCoreContext>()
            .UseSwimDocSqlite()
            .Options;
        Context = new EfCoreContext(options, connectionService);
    }

    [TearDown]
    public void TearDownDatabase()
    {
        Context.ChangeTracker.Clear();
        Context.Database.CloseConnection();
        Context.Dispose();
        SqliteConnection.ClearAllPools();
        TryDeleteFile(_dbPath);
        TryDeleteFile($"{_dbPath}-wal");
        TryDeleteFile($"{_dbPath}-shm");
    }

    private static void TryDeleteFile(string path)
    {
        if (!File.Exists(path))
            return;
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    protected static string ResourcePath(params string[] parts) =>
        Path.Combine([TestContext.CurrentContext.TestDirectory, ..parts]);
}
