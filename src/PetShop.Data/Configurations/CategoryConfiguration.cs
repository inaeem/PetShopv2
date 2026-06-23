using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetShop.Domain.Entities;

namespace PetShop.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Metadata).HasColumnType("nvarchar(max)");
        builder.Property(c => c.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
