using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetShop.Domain.Entities;

namespace PetShop.Data.Configurations;

public class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.ToTable("Plants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Species).HasMaxLength(100);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
