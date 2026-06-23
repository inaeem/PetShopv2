using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Adds the Plants table. Plants are a second kind of shop item alongside Pets and
/// are referenced (together with Pets) by the UsesGroups table added in the next
/// migration.
/// </summary>
public partial class AddPlantsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Plants",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Species = table.Column<string>(maxLength: 100, nullable: true),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                CreatedUtc = table.Column<DateTime>(nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Plants", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Plants");
    }
}
