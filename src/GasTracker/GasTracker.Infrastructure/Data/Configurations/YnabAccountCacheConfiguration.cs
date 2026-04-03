using GasTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GasTracker.Infrastructure.Data.Configurations;

public class YnabAccountCacheConfiguration : IEntityTypeConfiguration<YnabAccountCache>
{
    public void Configure(EntityTypeBuilder<YnabAccountCache> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.AccountId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Type).HasMaxLength(50).IsRequired();
    }
}
