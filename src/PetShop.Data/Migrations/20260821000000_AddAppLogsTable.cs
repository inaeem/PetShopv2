using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PetShop.Data.Context;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Adds the AppLogs table written by the Serilog MSSqlServer sink. The sink inserts rows
/// directly (SqlBulkCopy, outside EF and outside any transaction); the matching
/// <c>AppLog</c> entity and <c>AppLogConfiguration</c> exist so the application can read
/// its own logs. Keep the three in step — the lengths below, the EF configuration, and
/// the sink's columnOptions in appsettings.json all describe the same columns.
///
/// Column names are chosen by the app, not by Serilog: the sink's columnOptions maps
/// each log-event property onto the column below (SourceContext → Source, Elapsed →
/// DurationMs, and so on). Request/response bodies and the endpoint live inside
/// LogMessage; the separate columns are the ones worth filtering and indexing on.
/// </summary>
[DbContext(typeof(PetShopDbContext))]
[Migration("20260821000000_AddAppLogsTable")]
public partial class AddAppLogsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppLogs",
            columns: table => new
            {
                LogId = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),

                // Standard Serilog columns (renamed via columnOptions).
                LoggedAtUtc = table.Column<DateTime>(nullable: false),
                Severity    = table.Column<string>(maxLength: 16, nullable: true),
                LogMessage  = table.Column<string>(nullable: true),
                ErrorDetail = table.Column<string>(nullable: true),

                // Custom columns, each fed by a log-event property of a different name.
                EventType   = table.Column<string>(maxLength: 32, nullable: true),
                Source      = table.Column<string>(maxLength: 256, nullable: true),
                Operation   = table.Column<string>(maxLength: 256, nullable: true),
                DurationMs  = table.Column<double>(nullable: true),
                HttpStatus  = table.Column<int>(nullable: true),

                CorrelationRef    = table.Column<string>(maxLength: 64, nullable: true),
                AppRegistrationId = table.Column<string>(maxLength: 64, nullable: true),
                PayerId           = table.Column<string>(maxLength: 64, nullable: true),
                ProviderId        = table.Column<string>(maxLength: 64, nullable: true),
                UserEmail         = table.Column<string>(maxLength: 256, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AppLogs", x => x.LogId));

        // Every "show me this request" query filters on the correlation id; the time
        // index backs retention deletes and time-ranged dashboards.
        migrationBuilder.CreateIndex("IX_AppLogs_CorrelationRef", "AppLogs", "CorrelationRef");
        migrationBuilder.CreateIndex("IX_AppLogs_LoggedAtUtc", "AppLogs", "LoggedAtUtc");
        migrationBuilder.CreateIndex("IX_AppLogs_PayerId", "AppLogs", "PayerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AppLogs");
    }
}
