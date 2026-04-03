using GasTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GasTracker.Infrastructure.Data.Configurations;

public class YnabCategoryCacheConfiguration : IEntityTypeConfiguration<YnabCategoryCache>
{
    public void Configure(EntityTypeBuilder<YnabCategoryCache> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.CategoryId).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CategoryGroupName).HasMaxLength(200).IsRequired();
    }
}
