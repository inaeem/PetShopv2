using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Adds Pets.OwnerEmail — the email of the user who owns/registered a pet — so callers
/// can be scoped to their own pets. Nullable: existing rows have no owner. Indexed
/// because pets are filtered by owner.
/// </summary>
public partial class AddPetOwnerEmail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OwnerEmail",
            table: "Pets",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Pets_OwnerEmail",
            table: "Pets",
            column: "OwnerEmail");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Pets_OwnerEmail",
            table: "Pets");

        migrationBuilder.DropColumn(
            name: "OwnerEmail",
            table: "Pets");
    }
}
