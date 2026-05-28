-- ============================================================================
-- BASELINE AN EXISTING DATABASE
--
-- Use this when the target database ALREADY contains the pet shop schema (it was
-- created outside EF, or by 01_schema.sql), so that `dotnet ef database update`
-- does NOT try to recreate the tables — and never runs the baseline twice.
--
-- EF tracks applied migrations in dbo.__EFMigrationsHistory. If that table is
-- empty, `database update` assumes nothing is applied and runs InitialCreate,
-- which fails because the tables already exist. This script creates the history
-- table (if needed) and records InitialCreate as already applied. After running
-- it once, `database update` will apply ONLY migrations after the baseline
-- (e.g. AddSearchPetsProcedure) and is safe to re-run any number of times.
--
-- It is itself idempotent (guarded by IF NOT EXISTS), so re-running is harmless.
-- ============================================================================

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
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260101000000_InitialCreate')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260101000000_InitialCreate', N'8.0.6');
GO

-- OPTIONAL: if dbo.usp_SearchPets already exists in this database and is identical,
-- also baseline the procedure migration so it is not re-applied. Otherwise leave
-- it out and let `database update` run it (it uses CREATE OR ALTER, so re-running
-- is safe).
-- IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260101000100_AddSearchPetsProcedure')
--     INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
--     VALUES (N'20260101000100_AddSearchPetsProcedure', N'8.0.6');
-- GO
