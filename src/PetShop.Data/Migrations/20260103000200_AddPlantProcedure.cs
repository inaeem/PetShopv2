using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Adds dbo.usp_AddPlant — the single entry point for creating a plant. It validates its
/// inputs up front (throwing before any write), then in one transaction get-or-creates the
/// matching Category by its unique name and inserts the Plant. Any failure rolls the whole
/// thing back, so a plant is never created without its category and vice versa. The procedure
/// returns the inserted plant row. Kept in its own migration so it can be versioned
/// independently of schema changes (mirrors usp_SearchPets).
/// </summary>
public partial class AddPlantProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_AddPlant
    @Name         NVARCHAR(100),
    @Price        DECIMAL(18,2),
    @CategoryName NVARCHAR(100),
    @Species      NVARCHAR(100) = NULL,
    @Description  NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Validation: enforced before any write so nothing is persisted on bad input.
    IF @Name IS NULL OR LTRIM(RTRIM(@Name)) = N''
        THROW 50001, 'Plant name is required.', 1;

    IF @CategoryName IS NULL OR LTRIM(RTRIM(@CategoryName)) = N''
        THROW 50002, 'Category name is required.', 1;

    IF @Price IS NULL OR @Price < 0
        THROW 50003, 'Price must be zero or greater.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Persist the category too. Categories.Name is unique, so get-or-create:
        -- only insert when a category with this name does not already exist.
        IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = @CategoryName)
        BEGIN
            INSERT INTO dbo.Categories (Name, Description)
            VALUES (@CategoryName, @Description);
        END

        INSERT INTO dbo.Plants (Name, Species, Price)
        VALUES (@Name, @Species, @Price);

        DECLARE @NewPlantId INT = CAST(SCOPE_IDENTITY() AS INT);

        COMMIT TRANSACTION;

        SELECT  p.Id,
                p.Name,
                p.Species,
                p.Price,
                p.CreatedUtc
        FROM    dbo.Plants AS p
        WHERE   p.Id = @NewPlantId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;  -- re-raise to the caller so the failure surfaces as an error
    END CATCH
END;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_AddPlant;");
    }
}
