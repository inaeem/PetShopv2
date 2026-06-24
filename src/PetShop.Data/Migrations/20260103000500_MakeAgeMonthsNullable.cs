using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Reverts Pets.AgeMonths to nullable (undoing 20260103000400_MakeAgeMonthsNonNullable).
/// Existing values are preserved; the column simply stops enforcing NOT NULL. Note the
/// earlier backfill of NULL -> 0 is not undone — those rows keep their 0 value.
/// </summary>
public partial class MakeAgeMonthsNullable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Backfill any NULLs before re-tightening the column to NOT NULL.
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
}
