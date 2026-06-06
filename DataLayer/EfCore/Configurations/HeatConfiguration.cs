using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCore.Configurations;

public class HeatConfiguration : IEntityTypeConfiguration<Heat>
{
    public void Configure(EntityTypeBuilder<Heat> builder)
    {
        builder
            .HasOne(heat => heat.SwimEvent)
            .WithMany(e => e.Heats)
            .HasForeignKey(heat => heat.SwimEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
