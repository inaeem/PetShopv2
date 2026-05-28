using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Creates dbo.usp_SearchPets — the stored procedure the data layer invokes
/// directly (see PetRepository.SearchAsync). Kept in its own migration so the
/// procedure body can be versioned independently of the schema.
/// </summary>
public partial class AddSearchPetsProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_SearchPets
    @Term       NVARCHAR(100) = NULL,
    @CategoryId INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  p.Id,
            p.Name,
            p.Breed,
            p.Price,
            p.Status,
            c.Name AS CategoryName
    FROM    dbo.Pets AS p
    INNER JOIN dbo.Categories AS c ON c.Id = p.CategoryId
    WHERE  (@Term IS NULL OR p.Name LIKE '%' + @Term + '%' OR p.Breed LIKE '%' + @Term + '%')
      AND  (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
    ORDER BY p.Name;
END;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_SearchPets;");
    }
}
