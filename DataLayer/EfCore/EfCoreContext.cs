using DataLayer.EfClasses;
using DataLayer.EfCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfCore;

public sealed class EfCoreContext : DbContext
{
    private static readonly object TriggerInitLock = new();
    private static string? _triggersInitializedForConnection;
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
        ConfigureSqliteConcurrency();
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
        modelBuilder.HasDbFunction(typeof(SwimDocDbFunctions).GetMethod(nameof(SwimDocDbFunctions.ContainsIgnoreCase))!);
        base.OnModelCreating(modelBuilder);
    }

    private void ConfigureSqliteConcurrency()
    {
        Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        Database.ExecuteSqlRaw("PRAGMA busy_timeout=10000;");
    }

    private void EnsureTriggersCreated()
    {
        var connectionString = Database.GetConnectionString();
        lock (TriggerInitLock)
        {
            if (connectionString == _triggersInitializedForConnection)
                return;
            CreateTriggers();
            _triggersInitializedForConnection = connectionString;
        }
    }

    private void CreateTriggers()
    {
        Database.ExecuteSqlRaw("DROP TRIGGER IF EXISTS trg_entries_after_update_set_entry_status_when_unlinked;");
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER trg_entries_after_update_set_entry_status_when_unlinked
            AFTER UPDATE OF SwimEventId ON Entries
            WHEN NEW.SwimEventId IS NULL AND OLD.SwimEventId IS NOT NULL
            BEGIN
                UPDATE Entries
                SET Status = 0 -- ENTRY
                WHERE Id = NEW.Id;
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_entries_after_insert_update_swim_event_status
            AFTER INSERT ON Entries
            WHEN NEW.SwimEventId IS NOT NULL
            BEGIN
                UPDATE SwimEvents
                SET Status =
                    CASE
                        WHEN (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                            THEN 0 -- EMPTY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) > 0
                            THEN 1 -- ENTRY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 0) = 0
                            THEN 2 -- NOT_STARTED
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 2) = 0
                            THEN 4 -- OFFICIAL
                        ELSE 3 -- RUNNING
                    END
                WHERE Id = NEW.SwimEventId;
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_entries_after_update_update_swim_event_status
            AFTER UPDATE ON Entries
            WHEN NEW.SwimEventId IS NOT OLD.SwimEventId
                 OR (NEW.SwimEventId IS NULL AND OLD.SwimEventId IS NOT NULL)
                 OR (NEW.SwimEventId IS NOT NULL AND OLD.SwimEventId IS NULL)
            BEGIN
                UPDATE SwimEvents
                SET Status =
                    CASE
                        WHEN (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                            THEN 0 -- EMPTY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) > 0
                            THEN 1 -- ENTRY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 0) = 0
                            THEN 2 -- NOT_STARTED
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 2) = 0
                            THEN 4 -- OFFICIAL
                        ELSE 3 -- RUNNING
                    END
                WHERE Id IN (NEW.SwimEventId, OLD.SwimEventId);
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_entries_after_delete_update_swim_event_status
            AFTER DELETE ON Entries
            WHEN OLD.SwimEventId IS NOT NULL
            BEGIN
                UPDATE SwimEvents
                SET Status =
                    CASE
                        WHEN (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                            THEN 0 -- EMPTY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) > 0
                            THEN 1 -- ENTRY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 0) = 0
                            THEN 2 -- NOT_STARTED
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 2) = 0
                            THEN 4 -- OFFICIAL
                        ELSE 3 -- RUNNING
                    END
                WHERE Id = OLD.SwimEventId;
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heats_after_insert_update_swim_event_status
            AFTER INSERT ON Heats
            WHEN NEW.SwimEventId IS NOT NULL
            BEGIN
                UPDATE SwimEvents
                SET Status =
                    CASE
                        WHEN (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                            THEN 0 -- EMPTY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) > 0
                            THEN 1 -- ENTRY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 0) = 0
                            THEN 2 -- NOT_STARTED
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 2) = 0
                            THEN 4 -- OFFICIAL
                        ELSE 3 -- RUNNING
                    END
                WHERE Id = NEW.SwimEventId;
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heats_after_update_update_swim_event_status
            AFTER UPDATE ON Heats
            WHEN NEW.SwimEventId IS NOT OLD.SwimEventId
                 OR NEW.Status IS NOT OLD.Status
                 OR (NEW.SwimEventId IS NULL AND OLD.SwimEventId IS NOT NULL)
                 OR (NEW.SwimEventId IS NOT NULL AND OLD.SwimEventId IS NULL)
            BEGIN
                UPDATE SwimEvents
                SET Status =
                    CASE
                        WHEN (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                            THEN 0 -- EMPTY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) > 0
                            THEN 1 -- ENTRY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 0) = 0
                            THEN 2 -- NOT_STARTED
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 2) = 0
                            THEN 4 -- OFFICIAL
                        ELSE 3 -- RUNNING
                    END
                WHERE Id IN (NEW.SwimEventId, OLD.SwimEventId);
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heats_after_delete_update_swim_event_status
            AFTER DELETE ON Heats
            WHEN OLD.SwimEventId IS NOT NULL
            BEGIN
                UPDATE SwimEvents
                SET Status =
                    CASE
                        WHEN (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                            THEN 0 -- EMPTY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) = 0
                             AND (SELECT COUNT(*) FROM Entries e WHERE e.SwimEventId = SwimEvents.Id) > 0
                            THEN 1 -- ENTRY
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 0) = 0
                            THEN 2 -- NOT_STARTED
                        WHEN (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id) > 0
                             AND (SELECT COUNT(*) FROM Heats h WHERE h.SwimEventId = SwimEvents.Id AND h.Status != 2) = 0
                            THEN 4 -- OFFICIAL
                        ELSE 3 -- RUNNING
                    END
                WHERE Id = OLD.SwimEventId;
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heat_positions_after_insert
            AFTER INSERT ON HeatPositions
            BEGIN
                UPDATE Entries
                SET Status = 2
                WHERE Id = NEW.EntryId;
            END;
            """);
        Database.ExecuteSqlRaw("DROP TRIGGER IF EXISTS trg_heat_positions_after_delete;");
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER trg_heat_positions_after_delete
            AFTER DELETE ON HeatPositions
            BEGIN
                UPDATE Entries
                SET Status = CASE
                    WHEN (SELECT e.SwimEventId FROM Entries e WHERE e.Id = OLD.EntryId) IS NULL THEN 0 -- ENTRY
                    ELSE 1 -- EVENT
                END
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
                        AND (a.Gender = ag.Gender OR ag.Gender = 2) -- 2 = Mixed
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
                      OR (
                          -- Individual entry: validate by athlete against the age group.
                          RelayId IS NULL
                          AND AthleteId IS NOT NULL
                          AND NOT EXISTS (
                              SELECT 1
                              FROM Athletes a
                              JOIN AgeGroups ag ON ag.Id = NEW.AgeGroupId
                              WHERE a.Id = Entries.AthleteId
                                AND (a.Gender = ag.Gender OR ag.Gender = 2) -- 2 = Mixed
                                AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                                AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                          )
                      )
                      OR (
                          -- Relay entry: validate each relay position athlete against the age group.
                          RelayId IS NOT NULL
                          AND (
                              -- Important: do NOT unlink solely because positions aren't inserted yet
                              -- (cascade inserts may create Entry before RelayPositions).
                              EXISTS (
                                  SELECT 1
                                  FROM RelayPositions rp
                                  JOIN Athletes a ON a.Id = rp.AthleteId
                                  JOIN AgeGroups ag ON ag.Id = NEW.AgeGroupId
                                  WHERE rp.RelayId = Entries.RelayId
                                    AND NOT (
                                        (a.Gender = ag.Gender OR ag.Gender = 2) -- 2 = Mixed
                                        AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                                        AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                                    )
                              )
                          )
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
                        AND (a.Gender = ag.Gender OR ag.Gender = 2) -- 2 = Mixed
                        AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                        AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                  );
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_relay_positions_after_insert_validate_entry_event
            AFTER INSERT ON RelayPositions
            BEGIN
                UPDATE Entries
                SET SwimEventId = NULL,
                    Status = 0
                WHERE RelayId = NEW.RelayId
                  AND Status = 1
                  AND SwimEventId IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM RelayPositions rp
                      JOIN Athletes a ON a.Id = rp.AthleteId
                      JOIN SwimEvents se ON se.Id = Entries.SwimEventId
                      JOIN AgeGroups ag ON ag.Id = se.AgeGroupId
                      WHERE rp.RelayId = Entries.RelayId
                        AND NOT (
                            (a.Gender = ag.Gender OR ag.Gender = 2) -- 2 = Mixed
                            AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                            AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                        )
                  );
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_relay_positions_after_update_validate_entry_event
            AFTER UPDATE ON RelayPositions
            BEGIN
                UPDATE Entries
                SET SwimEventId = NULL,
                    Status = 0
                WHERE RelayId = NEW.RelayId
                  AND Status = 1
                  AND SwimEventId IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM RelayPositions rp
                      JOIN Athletes a ON a.Id = rp.AthleteId
                      JOIN SwimEvents se ON se.Id = Entries.SwimEventId
                      JOIN AgeGroups ag ON ag.Id = se.AgeGroupId
                      WHERE rp.RelayId = Entries.RelayId
                        AND NOT (
                            (a.Gender = ag.Gender OR ag.Gender = 2) -- 2 = Mixed
                            AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                            AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                        )
                  );
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_relay_positions_after_delete_validate_entry_event
            AFTER DELETE ON RelayPositions
            BEGIN
                UPDATE Entries
                SET SwimEventId = NULL,
                    Status = 0
                WHERE RelayId = OLD.RelayId
                  AND Status = 1
                  AND SwimEventId IS NOT NULL
                  AND (
                      NOT EXISTS (SELECT 1 FROM RelayPositions rp WHERE rp.RelayId = Entries.RelayId)
                      OR EXISTS (
                          SELECT 1
                          FROM RelayPositions rp
                          JOIN Athletes a ON a.Id = rp.AthleteId
                          JOIN SwimEvents se ON se.Id = Entries.SwimEventId
                          JOIN AgeGroups ag ON ag.Id = se.AgeGroupId
                          WHERE rp.RelayId = Entries.RelayId
                            AND NOT (
                                (a.Gender = ag.Gender OR ag.Gender = 2) -- 2 = Mixed
                                AND a.YearOfBirth >= COALESCE(ag.BirthYearMin, 0)
                                AND a.YearOfBirth <= COALESCE(ag.BirthYearMax, 2147483647)
                            )
                      )
                  );
            END;
            """);
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_swim_events_before_delete
            BEFORE DELETE ON SwimEvents
            BEGIN
                UPDATE Entries
                SET SwimEventId = NULL,
                    Status = 0 -- ENTRY
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
        Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_heats_after_update_reorder
            AFTER UPDATE OF Number ON Heats
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
