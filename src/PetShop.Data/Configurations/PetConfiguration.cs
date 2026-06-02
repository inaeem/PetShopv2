using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetShop.Domain.Entities;

namespace PetShop.Data.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Breed).HasMaxLength(100);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.OwnerEmail).HasMaxLength(256);
        builder.Property(p => p.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Pets)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.OwnerEmail);
    }
}
