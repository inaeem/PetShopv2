using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Adds the UsesGroups table. Each row links to exactly one subject via a polymorphic
/// reference: Type (1 = Pet, 2 = Plant) selects the table and SubjectId is the row's key
/// in it. There is intentionally no foreign key — a single column can't target two tables,
/// so SubjectId integrity is the application's responsibility. (Type, SubjectId) is indexed
/// because rows are looked up by their subject.
/// </summary>
public partial class CreateUsesGroupTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UsesGroups",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                Type = table.Column<int>(nullable: false),
                SubjectId = table.Column<int>(nullable: false),
                CreatedUtc = table.Column<DateTime>(nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table => table.PrimaryKey("PK_UsesGroups", x => x.Id));

        migrationBuilder.CreateIndex("IX_UsesGroups_Type_SubjectId", "UsesGroups",
            new[] { "Type", "SubjectId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("UsesGroups");
    }
}
