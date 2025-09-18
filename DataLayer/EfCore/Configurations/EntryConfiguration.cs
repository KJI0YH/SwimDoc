using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCore.Configurations;

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder
            .HasOne(entry => entry.SwimEvent)
            .WithMany(swimEvent => swimEvent.Entries)
            .HasForeignKey(entry => entry.SwimEventId);
        builder
            .HasOne(entry => entry.HeatPosition)
            .WithOne(heatPosition => heatPosition.Entry)
            .HasForeignKey<Entry>(entry => entry.HeatPositionId)
            .IsRequired(false);
        builder.HasOne(entry => entry.Athlete)
            .WithMany(athlete => athlete.Entries)
            .HasForeignKey(entry => entry.AthleteId);
        builder.HasOne(entry => entry.Relay)
            .WithOne(relay => relay.Entry)
            .HasForeignKey<Entry>(entry => entry.RelayId);
    }
}