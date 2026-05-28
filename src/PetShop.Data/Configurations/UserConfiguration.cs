using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetShop.Domain.Entities;

namespace PetShop.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Roles).HasMaxLength(256).IsRequired();
        builder.Property(u => u.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(u => u.Username).IsUnique();
    }
}

/// <summary>
/// Configures the keyless stored-procedure result. Keyless types must not be
/// treated as tables, so the entity is excluded from migrations.
/// </summary>
public class PetSearchResultConfiguration : IEntityTypeConfiguration<StoredProcedures.PetSearchResult>
{
    public void Configure(EntityTypeBuilder<StoredProcedures.PetSearchResult> builder)
    {
        builder.HasNoKey();
        builder.ToView(null); // not backed by a table or view; populated via FromSql
    }
}
