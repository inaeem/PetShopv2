using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetShop.Domain.Entities;

namespace PetShop.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Action).HasMaxLength(200).IsRequired();
        builder.Property(l => l.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        // Foreign keys on PetId (pre-existing) and CategoryId (newly added), declared without
        // navigation properties. Both are optional (nullable) so NULLs are allowed; NO ACTION
        // on delete avoids cascading into the log. In the database these are created WITH
        // NOCHECK (see the migration) so existing data is never validated, but EF only tracks
        // that the relationship exists. EF creates the backing index for each FK column.
        builder.HasOne<Pet>().WithMany().HasForeignKey(l => l.PetId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Category>().WithMany().HasForeignKey(l => l.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
