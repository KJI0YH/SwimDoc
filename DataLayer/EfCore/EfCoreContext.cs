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
        EnsureTriggersCreated();
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

    private void EnsureTriggersCreated()
    {
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heat_positions_after_insert
            AFTER INSERT ON HeatPositions
            BEGIN
                UPDATE Entries
                SET Status = 2
                WHERE Id = NEW.EntryId;
            END;
            """);

        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heat_positions_after_delete
            AFTER DELETE ON HeatPositions
            BEGIN
                UPDATE Entries
                SET Status = 1
                WHERE Id = OLD.EntryId;
            END;
            """);

        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_swim_events_after_insert
            AFTER INSERT ON SwimEvents
            BEGIN
                UPDATE Entries
                SET SwimEventId = NEW.Id,
                    Status = 1
                WHERE Status = 0
                  AND SwimStyleId = NEW.SwimStyleId
                  AND AthleteId IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM Athletes a
                      JOIN AgeGroups ag ON ag.Id = NEW.AgeGroupId
                      WHERE a.Id = Entries.AthleteId
                        AND a.Gender = ag.Gender
                        AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                        AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                  );
            END;
            """);

        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_swim_events_after_update
            AFTER UPDATE ON SwimEvents
            BEGIN
                UPDATE Entries
                SET SwimEventId = NULL,
                    Status = 0
                WHERE SwimEventId = NEW.Id
                  AND Status = 1
                  AND (
                      SwimStyleId != NEW.SwimStyleId
                      OR AthleteId IS NULL
                      OR NOT EXISTS (
                          SELECT 1
                          FROM Athletes a
                          JOIN AgeGroups ag ON ag.Id = NEW.AgeGroupId
                          WHERE a.Id = Entries.AthleteId
                            AND a.Gender = ag.Gender
                            AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                            AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                      )
                  );

                UPDATE Entries
                SET SwimEventId = NEW.Id,
                    Status = 1
                WHERE Status = 0
                  AND SwimStyleId = NEW.SwimStyleId
                  AND AthleteId IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM Athletes a
                      JOIN AgeGroups ag ON ag.Id = NEW.AgeGroupId
                      WHERE a.Id = Entries.AthleteId
                        AND a.Gender = ag.Gender
                        AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                        AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                  );
            END;
            """);

        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_swim_events_before_delete
            BEFORE DELETE ON SwimEvents
            BEGIN
                UPDATE Entries
                SET SwimEventId = NULL,
                    Status = 0
                WHERE SwimEventId = OLD.Id;
            END;
            """);

        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heats_after_insert_reorder
            AFTER INSERT ON Heats
            BEGIN
                UPDATE Heats
                SET "Order" = (
                    SELECT COUNT(*)
                    FROM Heats h2
                    JOIN SwimEvents se2 ON se2.Id = h2.SwimEventId
                    WHERE se2."Order" < (SELECT se1."Order" FROM SwimEvents se1 WHERE se1.Id = Heats.SwimEventId)
                       OR (
                           se2."Order" = (SELECT se1."Order" FROM SwimEvents se1 WHERE se1.Id = Heats.SwimEventId)
                           AND (
                               h2.Number < Heats.Number
                               OR (h2.Number = Heats.Number AND h2.Id <= Heats.Id)
                           )
                       )
                );
            END;
            """);

        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heats_after_delete_reorder
            AFTER DELETE ON Heats
            BEGIN
                UPDATE Heats
                SET "Order" = (
                    SELECT COUNT(*)
                    FROM Heats h2
                    JOIN SwimEvents se2 ON se2.Id = h2.SwimEventId
                    WHERE se2."Order" < (SELECT se1."Order" FROM SwimEvents se1 WHERE se1.Id = Heats.SwimEventId)
                       OR (
                           se2."Order" = (SELECT se1."Order" FROM SwimEvents se1 WHERE se1.Id = Heats.SwimEventId)
                           AND (
                               h2.Number < Heats.Number
                               OR (h2.Number = Heats.Number AND h2.Id <= Heats.Id)
                           )
                       )
                );
            END;
            """);
    }
}