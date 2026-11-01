using GasTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GasTracker.Infrastructure.Data.Configurations;

public class VehicleMemoMappingConfiguration : IEntityTypeConfiguration<VehicleMemoMapping>
{
    public void Configure(EntityTypeBuilder<VehicleMemoMapping> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.MemoName).HasMaxLength(200).IsRequired();
        // Memo names are unique per household; the composite index also serves household lookups.
        builder.HasIndex(m => new { m.HouseholdId, m.MemoName }).IsUnique();

        builder.HasOne(m => m.Vehicle)
            .WithMany()
            .HasForeignKey(m => m.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Household)
            .WithMany()
            .HasForeignKey(m => m.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
