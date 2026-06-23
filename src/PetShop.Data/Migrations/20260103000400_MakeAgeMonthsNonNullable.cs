using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Makes Pets.AgeMonths non-nullable. Existing rows with a NULL age are backfilled
/// to 0 first so the column can be tightened to NOT NULL without error.
/// </summary>
public partial class MakeAgeMonthsNonNullable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Backfill existing NULLs before tightening the column to NOT NULL.
        migrationBuilder.Sql("UPDATE [Pets] SET [AgeMonths] = 0 WHERE [AgeMonths] IS NULL;");

        migrationBuilder.AlterColumn<int>(
            name: "AgeMonths",
            table: "Pets",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "AgeMonths",
            table: "Pets",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: false);
    }
}
