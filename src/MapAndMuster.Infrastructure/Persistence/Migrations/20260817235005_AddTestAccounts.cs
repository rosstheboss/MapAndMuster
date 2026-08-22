using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTestAccounts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsTestAccount",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "TestAccountNumber",
            table: "AspNetUsers",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_TestAccountNumber",
            table: "AspNetUsers",
            column: "TestAccountNumber",
            unique: true,
            filter: "\"TestAccountNumber\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_TestAccountNumber",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "IsTestAccount",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "TestAccountNumber",
            table: "AspNetUsers");
    }
}
