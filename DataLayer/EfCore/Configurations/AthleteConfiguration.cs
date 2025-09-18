using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCore.Configurations;

public class AthleteConfiguration : IEntityTypeConfiguration<Athlete>
{
    public void Configure(EntityTypeBuilder<Athlete> builder)
    {
        builder
            .Property(athlete => athlete.Gender)
            .HasConversion<int>();
        builder
            .HasOne(athlete => athlete.Club)
            .WithMany(club => club.Athletes)
            .HasForeignKey(athlete => athlete.ClubId);
    }
}