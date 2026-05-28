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
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FullName = table.Column<string>(maxLength: 150, nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                Phone = table.Column<string>(maxLength: 30, nullable: true),
                CreatedUtc = table.Column<DateTime>(nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));

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

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CustomerId = table.Column<int>(nullable: false),
                Status = table.Column<int>(nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                OrderedUtc = table.Column<DateTime>(nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.ForeignKey("FK_Orders_Customers_CustomerId", x => x.CustomerId,
                    "Customers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "OrderItems",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OrderId = table.Column<int>(nullable: false),
                PetId = table.Column<int>(nullable: false),
                Quantity = table.Column<int>(nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderItems", x => x.Id);
                table.ForeignKey("FK_OrderItems_Orders_OrderId", x => x.OrderId,
                    "Orders", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_OrderItems_Pets_PetId", x => x.PetId,
                    "Pets", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Categories_Name", "Categories", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Customers_Email", "Customers", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Users_Username", "Users", "Username", unique: true);
        migrationBuilder.CreateIndex("IX_Pets_Status", "Pets", "Status");
        migrationBuilder.CreateIndex("IX_Pets_CategoryId", "Pets", "CategoryId");
        migrationBuilder.CreateIndex("IX_Orders_CustomerId", "Orders", "CustomerId");
        migrationBuilder.CreateIndex("IX_OrderItems_OrderId", "OrderItems", "OrderId");
        migrationBuilder.CreateIndex("IX_OrderItems_PetId", "OrderItems", "PetId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("OrderItems");
        migrationBuilder.DropTable("Orders");
        migrationBuilder.DropTable("Pets");
        migrationBuilder.DropTable("Users");
        migrationBuilder.DropTable("Customers");
        migrationBuilder.DropTable("Categories");
    }
}
