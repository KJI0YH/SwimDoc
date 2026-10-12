using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.ConnectionService;
using Tests.TestInfrastructure;

namespace Tests.Event;

[TestFixture]
public class SwimEventSchemaUpgradeTest : DatabaseTestFixture
{
    [Test]
    public void OpeningDatabaseWithoutPointsMultiplier_AddsColumnWithDefaultOne()
    {
        var seeder = new TestDataSeeder(Context);
        var swimEvent = seeder.SeedSwimEvent(seeder.SeedAgeGroup(), seeder.SeedSwimStyle());
        Context.Database.ExecuteSqlRaw("ALTER TABLE SwimEvents DROP COLUMN PointsMultiplier;");
        var dataSource = Context.Database.GetConnectionString()!;

        var connectionService = new DatabaseConnectionService();
        connectionService.SetConnection($"{dataSource};Pooling=False");
        var options = new DbContextOptionsBuilder<EfCoreContext>().UseSwimDocSqlite().Options;
        using var upgraded = new EfCoreContext(options, connectionService);

        var multiplier = upgraded.SwimEvents.AsNoTracking().Single(e => e.Id == swimEvent.Id).PointsMultiplier;
        Assert.That(multiplier, Is.EqualTo(1));
        upgraded.Database.CloseConnection();
    }
}
