using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCore.Configurations;

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder
            .HasOne(entry => entry.SwimStyle)
            .WithMany(swimStyle => swimStyle.Entries)
            .HasForeignKey(entry => entry.SwimStyleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(entry => entry.SwimEvent)
            .WithMany(swimEvent => swimEvent.Entries)
            .HasForeignKey(entry => entry.SwimEventId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(entry => entry.Athlete)
            .WithMany(athlete => athlete.Entries)
            .HasForeignKey(entry => entry.AthleteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(entry => entry.Relay)
            .WithOne(relay => relay.Entry)
            .HasForeignKey<Entry>(entry => entry.RelayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}