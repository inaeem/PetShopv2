using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Adds a nullable CategoryId column to the existing ActivityLogs table, plus a foreign key to
/// Categories. The table and its PetId foreign key already exist and are left untouched — this
/// migration only extends the table. CategoryId is new, so every existing row has
/// CategoryId = NULL; the FK is added WITH NOCHECK so existing data is never validated, while
/// new writes are enforced. Every step is guarded so the migration is safe and idempotent.
/// </summary>
public partial class AddCategoryIdToActivityLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Add the new nullable CategoryId column if it isn't already there.
        migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.ActivityLogs','CategoryId') IS NULL
    ALTER TABLE [dbo].[ActivityLogs] ADD [CategoryId] int NULL;");

        // 2. Foreign key to Categories, added WITH NOCHECK: existing rows (all NULL for the new
        //    column) are not validated and new writes are enforced. PetId is NOT touched.
        migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ActivityLogs_Categories_CategoryId')
    ALTER TABLE [dbo].[ActivityLogs] WITH NOCHECK ADD CONSTRAINT [FK_ActivityLogs_Categories_CategoryId]
        FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([Id]) ON DELETE NO ACTION;");

        // 3. Index the new FK column for lookups/joins.
        migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActivityLogs_CategoryId' AND object_id = OBJECT_ID('dbo.ActivityLogs'))
    CREATE INDEX [IX_ActivityLogs_CategoryId] ON [dbo].[ActivityLogs] ([CategoryId]);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse only the CategoryId addition; leave the table and PetId FK intact.
        migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ActivityLogs_Categories_CategoryId')
    ALTER TABLE [dbo].[ActivityLogs] DROP CONSTRAINT [FK_ActivityLogs_Categories_CategoryId];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActivityLogs_CategoryId' AND object_id = OBJECT_ID('dbo.ActivityLogs'))
    DROP INDEX [IX_ActivityLogs_CategoryId] ON [dbo].[ActivityLogs];
IF COL_LENGTH('dbo.ActivityLogs','CategoryId') IS NOT NULL
    ALTER TABLE [dbo].[ActivityLogs] DROP COLUMN [CategoryId];");
    }
}
