using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetShop.Domain.Entities;

namespace PetShop.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
