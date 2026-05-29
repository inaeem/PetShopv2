using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Data.Migrations;

/// <summary>
/// Removes the Customers, Orders and OrderItems tables now that the project scope is
/// Pets-only. Intended for databases created by the ORIGINAL schema (which had these
/// tables). The drops are guarded with IF OBJECT_ID checks so this migration is a safe
/// no-op on a fresh database (where the trimmed InitialCreate never created them) and
/// on a DB where it has already been applied.
///
/// This is destructive: it deletes the Customers/Orders/OrderItems data. Back up first
/// if those rows matter. Down() recreates the (empty) tables for rollback symmetry; it
/// does not restore data.
/// </summary>
public partial class DropOrderTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop dependents before principals (OrderItems → Orders → Customers).
        migrationBuilder.Sql("IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;");
        migrationBuilder.Sql("IF OBJECT_ID(N'dbo.Orders', N'U')     IS NOT NULL DROP TABLE dbo.Orders;");
        migrationBuilder.Sql("IF OBJECT_ID(N'dbo.Customers', N'U')  IS NOT NULL DROP TABLE dbo.Customers;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Recreate principals before dependents (Customers → Orders → OrderItems).
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

        migrationBuilder.CreateIndex("IX_Customers_Email", "Customers", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Orders_CustomerId", "Orders", "CustomerId");
        migrationBuilder.CreateIndex("IX_OrderItems_OrderId", "OrderItems", "OrderId");
        migrationBuilder.CreateIndex("IX_OrderItems_PetId", "OrderItems", "PetId");
    }
}
