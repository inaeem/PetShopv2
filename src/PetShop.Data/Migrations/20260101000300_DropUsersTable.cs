using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Removes the Users table. The API no longer issues tokens or stores users — clients
/// authenticate with an externally-issued JWT validated against the shared signing key
/// — so the local credential store is gone. Intended for databases created by the
/// ORIGINAL schema (which had this table); the drop is guarded with an IF OBJECT_ID
/// check so it is a safe no-op on a fresh database (where the trimmed InitialCreate
/// never created it) and on a DB where it has already been applied.
///
/// This is destructive: it deletes the Users data. Down() recreates the (empty) table
/// for rollback symmetry; it does not restore data.
/// </summary>
public partial class DropUsersTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Username = table.Column<string>(maxLength: 100, nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(maxLength: 512, nullable: false),
                Roles = table.Column<string>(maxLength: 256, nullable: false),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                CreatedUtc = table.Column<DateTime>(nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

        migrationBuilder.CreateIndex("IX_Users_Username", "Users", "Username", unique: true);
    }
}
