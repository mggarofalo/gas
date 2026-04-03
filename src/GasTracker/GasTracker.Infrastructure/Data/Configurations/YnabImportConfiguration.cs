using GasTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GasTracker.Infrastructure.Data.Configurations;

public class YnabImportConfiguration : IEntityTypeConfiguration<YnabImport>
{
    public void Configure(EntityTypeBuilder<YnabImport> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.YnabTransactionId).HasMaxLength(100).IsRequired();
        builder.HasIndex(i => i.YnabTransactionId).IsUnique();

        builder.Property(i => i.PayeeName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Memo).HasMaxLength(500);
        builder.Property(i => i.VehicleName).HasMaxLength(200);
        builder.Property(i => i.Status).HasMaxLength(20).HasDefaultValue("pending");

        builder.Property(i => i.Gallons).HasPrecision(10, 3);
        builder.Property(i => i.PricePerGallon).HasPrecision(10, 3);

        builder.HasIndex(i => i.Status);
    }
}
