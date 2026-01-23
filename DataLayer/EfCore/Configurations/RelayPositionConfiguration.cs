using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCore.Configurations;

public class RelayPositionConfiguration : IEntityTypeConfiguration<RelayPosition>
{
    public void Configure(EntityTypeBuilder<RelayPosition> builder)
    {
        builder
            .HasKey(relayPos => new { relayPos.AthleteId, relayPos.RelayId });
        builder
            .HasOne(relayPosition => relayPosition.Athlete)
            .WithMany(athlete => athlete.RelayPositions)
            .HasForeignKey(relayPosition => relayPosition.AthleteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(relayPosition => relayPosition.Relay)
            .WithMany(relay => relay.Positions)
            .HasForeignKey(relayPosition => relayPosition.RelayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}