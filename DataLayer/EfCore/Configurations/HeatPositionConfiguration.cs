using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCore.Configurations;

public class HeatPositionConfiguration : IEntityTypeConfiguration<HeatPosition>
{
    public void Configure(EntityTypeBuilder<HeatPosition> builder)
    {
        builder
            .HasKey(position => new { position.HeatId, position.EntryId, position.Lane });
        builder
            .HasOne(heatPosition => heatPosition.Entry)
            .WithOne(entry => entry.HeatPosition)
            .HasForeignKey<HeatPosition>(heatPosition => heatPosition.EntryId);
        builder
            .HasOne(heatPosition => heatPosition.Heat)
            .WithMany(heat => heat.Positions)
            .HasForeignKey(position => position.HeatId);
    }
}