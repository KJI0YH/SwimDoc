using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCore.Configurations;

public class SwimEventConfiguration : IEntityTypeConfiguration<SwimEvent>
{
    public void Configure(EntityTypeBuilder<SwimEvent> builder)
    {
        builder
            .HasOne(e => e.SwimStyle)
            .WithMany(style => style.Events)
            .HasForeignKey(e => e.SwimStyleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(e => e.AgeGroup)
            .WithMany(group => group.Events)
            .HasForeignKey(e => e.AgeGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(e => e.PreviousSwimEvent)
            .WithOne(e => e.NextSwimEvent)
            .HasForeignKey<SwimEvent>(e => e.PreviousSwimEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.CustomLaneNames).HasMaxLength(500);
    }
}
