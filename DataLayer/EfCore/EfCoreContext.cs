using DataLayer.EfClasses;
using DataLayer.EfCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfCore;

public sealed class EfCoreContext : DbContext
{
    public DbSet<AgeGroup> AgeGroups { get; set; }
    public DbSet<Athlete> Athletes { get; set; }
    public DbSet<Club> Clubs { get; set; }
    public DbSet<Entry> Entries { get; set; }
    public DbSet<SwimEvent> SwimEvents { get; set; }
    public DbSet<Heat> Heats { get; set; }
    public DbSet<HeatPosition> HeatPositions { get; set; }
    public DbSet<Relay> Relays { get; set; }
    public DbSet<RelayPosition> RelayPositions { get; set; }
    public DbSet<SwimStyle> SwimStyles { get; set; }

    public EfCoreContext(DbContextOptions<EfCoreContext> options, IDatabaseConnection databaseConnection) : base(options)
    {
        Database.SetConnectionString(databaseConnection?.CurrentConnection());
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AthleteConfiguration());
        modelBuilder.ApplyConfiguration(new EntryConfiguration());
        modelBuilder.ApplyConfiguration(new SwimEventConfiguration());
        modelBuilder.ApplyConfiguration(new HeatConfiguration());
        modelBuilder.ApplyConfiguration(new HeatPositionConfiguration());
        modelBuilder.ApplyConfiguration(new RelayConfiguration());
        modelBuilder.ApplyConfiguration(new RelayPositionConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}