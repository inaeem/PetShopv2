using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetShop.Domain.Entities;

namespace PetShop.Data.Configurations;

/// <summary>
/// Maps <see cref="AppLog"/> onto dbo.AppLogs. The lengths here must match the ones the
/// Serilog sink is configured with in appsettings.json ("columnOptionsSection"), because
/// the sink writes the rows and this model only reads them: a column widened here but not
/// there silently truncates, and the reverse throws on insert inside the sink.
/// <para>
/// Every column except the key and the timestamp is nullable by design — a row written
/// before a value was known (correlation established, payer resolved, user authenticated)
/// carries NULL there rather than being dropped.
/// </para>
/// </summary>
public class AppLogConfiguration : IEntityTypeConfiguration<AppLog>
{
    public void Configure(EntityTypeBuilder<AppLog> builder)
    {
        builder.ToTable("AppLogs");
        builder.HasKey(l => l.LogId);
        builder.Property(l => l.LogId).ValueGeneratedOnAdd();

        builder.Property(l => l.Severity).HasMaxLength(16);
        builder.Property(l => l.EventType).HasMaxLength(32);
        builder.Property(l => l.Source).HasMaxLength(256);
        builder.Property(l => l.Operation).HasMaxLength(256);

        // LogMessage and ErrorDetail are nvarchar(max): bodies and stack traces have no
        // useful upper bound, and the writer truncates them long before SQL Server would.
        builder.Property(l => l.CorrelationRef).HasMaxLength(64);
        builder.Property(l => l.AppRegistrationId).HasMaxLength(64);
        builder.Property(l => l.PayerId).HasMaxLength(64);
        builder.Property(l => l.ProviderId).HasMaxLength(64);
        builder.Property(l => l.UserEmail).HasMaxLength(256);

        // Index names are left to EF's convention (IX_AppLogs_<column>) so they match the
        // ones the migration creates. CorrelationRef backs "show me this whole request",
        // LoggedAtUtc backs time-ranged reads and retention deletes, PayerId backs the
        // per-payer history screens.
        builder.HasIndex(l => l.CorrelationRef);
        builder.HasIndex(l => l.LoggedAtUtc);
        builder.HasIndex(l => l.PayerId);
    }
}
