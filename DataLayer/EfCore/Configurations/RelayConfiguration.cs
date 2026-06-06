using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCore.Configurations;

public class RelayConfiguration : IEntityTypeConfiguration<Relay>
{
    public void Configure(EntityTypeBuilder<Relay> builder)
    {
        builder
            .HasOne(relay => relay.Club)
            .WithMany(club => club.Relays)
            .HasForeignKey(relay => relay.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
