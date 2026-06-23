using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Adds Categories.Metadata — free-form metadata (e.g. JSON) attached to a category.
/// Nullable nvarchar(max): existing rows have no metadata and callers may store
/// arbitrary structured data.
/// </summary>
public partial class AddCategoryMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Metadata",
            table: "Categories",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Metadata",
            table: "Categories");
    }
}
