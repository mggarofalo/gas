using GasTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GasTracker.Infrastructure.Data.Configurations;

public class YnabSettingsConfiguration : IEntityTypeConfiguration<YnabSettings>
{
    public void Configure(EntityTypeBuilder<YnabSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.ApiToken).HasMaxLength(500).IsRequired();
        builder.Property(s => s.PlanId).HasMaxLength(100);
        builder.Property(s => s.PlanName).HasMaxLength(200);
        builder.Property(s => s.AccountId).HasMaxLength(100);
        builder.Property(s => s.AccountName).HasMaxLength(200);
        builder.Property(s => s.CategoryId).HasMaxLength(100);
        builder.Property(s => s.CategoryName).HasMaxLength(200);
        builder.Property(s => s.Enabled).HasDefaultValue(false);
    }
}
