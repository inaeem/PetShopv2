using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Baseline migration matching the existing pet shop database. If you are
/// reverse-engineering a live database, generate this once with
/// <c>dotnet ef migrations add InitialCreate</c> and then apply with
/// <c>dotnet ef database update</c>. The hand-written version below mirrors the
/// entity configurations so the project builds and runs out of the box.
/// </summary>
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                CreatedUtc = table.Column<DateTime>(nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table => table.PrimaryKey("PK_Categories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Pets",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Breed = table.Column<string>(maxLength: 100, nullable: true),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                AgeMonths = table.Column<int>(nullable: true),
                Status = table.Column<int>(nullable: false),
                CategoryId = table.Column<int>(nullable: false),
                CreatedUtc = table.Column<DateTime>(nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Pets", x => x.Id);
                table.ForeignKey("FK_Pets_Categories_CategoryId", x => x.CategoryId,
                    "Categories", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Categories_Name", "Categories", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Pets_Status", "Pets", "Status");
        migrationBuilder.CreateIndex("IX_Pets_CategoryId", "Pets", "CategoryId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Pets");
        migrationBuilder.DropTable("Categories");
    }
}
