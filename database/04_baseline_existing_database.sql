-- ============================================================================
-- BASELINE AN EXISTING DATABASE  (reusable template)
--
-- Use this when the target database ALREADY contains the schema (created outside
-- EF, by an existing DBA-owned script, or by 01_schema.sql), so that
-- `dotnet ef database update` does NOT try to recreate the tables — and never
-- runs the baseline twice.
--
-- EF tracks applied migrations in dbo.__EFMigrationsHistory. If that table is
-- empty, `database update` assumes nothing is applied and runs InitialCreate,
-- which FAILS because the tables already exist. This script creates the history
-- table (if needed) and records InitialCreate as already applied. After running
-- it once, `database update` applies ONLY migrations created after the baseline,
-- and this script is safe to re-run any number of times.
--
-- ----------------------------------------------------------------------------
-- HOW TO USE — pass the two values as sqlcmd variables (-v), nothing to edit:
--
--   $(MigrationId)    the InitialCreate migration id
--                     (file name, without .cs, of Migrations/*_InitialCreate.cs)
--   $(ProductVersion) the EF Core version (from .config/dotnet-tools.json)
--
--   sqlcmd -S <server> -d <Db> -i database/04_baseline_existing_database.sql ^
--          -v MigrationId="20260101000000_InitialCreate" ProductVersion="8.0.6"
--
-- For THIS (PetShop) project the values are exactly the two shown above. When you
-- reuse this file in a ported project, derive the id from the generated file:
--
--   $mig = (Get-ChildItem "src/<Proj>.Data/Migrations/*_InitialCreate.cs").BaseName
--   sqlcmd -S <server> -d <Db> -i database/04_baseline_existing_database.sql `
--          -v MigrationId="$mig" ProductVersion="8.0.6"
--
-- This script is idempotent (guarded by IF NOT EXISTS), so re-running is harmless.
-- ============================================================================

-- Fail fast if the caller forgot to supply the required variables.
:on error exit
IF N'$(MigrationId)' = N'' OR N'$(ProductVersion)' = N''
BEGIN
    RAISERROR(N'Set the MigrationId and ProductVersion sqlcmd variables (-v). See header.', 16, 1);
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId]    nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

-- Mark the baseline schema migration as already applied.
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'$(MigrationId)')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'$(MigrationId)', N'$(ProductVersion)');
GO

-- OPTIONAL: if the database already contains objects from more than one
-- pre-existing migration (e.g. a stored-procedure migration whose objects exist
-- and are identical — in PetShop that is 20260101000100_AddSearchPetsProcedure),
-- copy the block below for each additional id to baseline. Otherwise leave them
-- out and let `dotnet ef database update` apply them (usp_SearchPets uses
-- CREATE OR ALTER, so re-running it is safe).
--
-- IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'<OTHER_MIGRATION_ID>')
--     INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
--     VALUES (N'<OTHER_MIGRATION_ID>', N'$(ProductVersion)');
-- GO

SET NOEXEC OFF;
GO
