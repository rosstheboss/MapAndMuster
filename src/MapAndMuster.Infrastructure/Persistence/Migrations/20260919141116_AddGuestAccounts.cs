using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddGuestAccounts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "GuestAccountNumber",
            table: "AspNetUsers",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "GuestExpiresUtc",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsGuestAccount",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_GuestAccountNumber",
            table: "AspNetUsers",
            column: "GuestAccountNumber",
            unique: true,
            filter: "\"GuestAccountNumber\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_GuestExpiresUtc",
            table: "AspNetUsers",
            column: "GuestExpiresUtc",
            filter: "\"IsGuestAccount\" = TRUE");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_GuestAccountNumber",
            table: "AspNetUsers");

        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_GuestExpiresUtc",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "GuestAccountNumber",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "GuestExpiresUtc",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "IsGuestAccount",
            table: "AspNetUsers");
    }
}
