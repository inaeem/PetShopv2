using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetShop.Domain.Entities;

namespace PetShop.Data.Configurations;

public class UsesGroupConfiguration : IEntityTypeConfiguration<UsesGroup>
{
    public void Configure(EntityTypeBuilder<UsesGroup> builder)
    {
        builder.ToTable("UsesGroups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(500);
        builder.Property(g => g.Type).HasConversion<int>();
        builder.Property(g => g.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        // No foreign key: SubjectId points at either Pets or Plants depending on Type,
        // so referential integrity is enforced in the application, not the database.
        // Index the discriminator + key together since rows are looked up by their subject.
        builder.HasIndex(g => new { g.Type, g.SubjectId });
    }
}
