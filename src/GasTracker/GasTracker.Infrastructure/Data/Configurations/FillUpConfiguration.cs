using GasTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GasTracker.Infrastructure.Data.Configurations;

public class FillUpConfiguration : IEntityTypeConfiguration<FillUp>
{
    public void Configure(EntityTypeBuilder<FillUp> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.Gallons).HasPrecision(8, 3).IsRequired();
        builder.Property(f => f.PricePerGallon).HasPrecision(6, 3).IsRequired();
        builder.Property(f => f.TotalCost).HasPrecision(8, 2).IsRequired();
        builder.Property(f => f.StationName).HasMaxLength(200).IsRequired();
        builder.Property(f => f.StationAddress).HasMaxLength(500);
        builder.Property(f => f.Latitude).HasPrecision(10, 7);
        builder.Property(f => f.Longitude).HasPrecision(10, 7);
        builder.Property(f => f.ReceiptPath).HasMaxLength(500);

        builder.Property(f => f.PaperlessSyncStatus).HasMaxLength(20).HasDefaultValue("none");
        builder.Property(f => f.PaperlessSyncAttempts).HasDefaultValue((short)0);

        builder.HasOne(f => f.Vehicle)
            .WithMany(v => v.FillUps)
            .HasForeignKey(f => f.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.YnabSyncStatus).HasMaxLength(20).HasDefaultValue("none");
        builder.Property(f => f.YnabTransactionId).HasMaxLength(100);
        builder.Property(f => f.YnabSyncError).HasMaxLength(500);
        builder.Property(f => f.YnabAccountId).HasMaxLength(100);
        builder.Property(f => f.YnabAccountName).HasMaxLength(200);

        builder.HasIndex(f => new { f.VehicleId, f.Date })
            .IsDescending(false, true)
            .HasDatabaseName("ix_fill_ups_vehicle_date");

        builder.HasIndex(f => f.Date)
            .IsDescending()
            .HasDatabaseName("ix_fill_ups_date");

        builder.HasIndex(f => f.YnabTransactionId)
            .IsUnique()
            .HasFilter("ynab_transaction_id IS NOT NULL")
            .HasDatabaseName("ix_fill_ups_ynab_transaction_id");
    }
}
