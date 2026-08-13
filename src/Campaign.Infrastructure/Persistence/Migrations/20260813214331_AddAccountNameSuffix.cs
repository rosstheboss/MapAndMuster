using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campaign.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAccountNameSuffix : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Suffix",
            table: "AspNetUsers",
            type: "character varying(8)",
            maxLength: 8,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Suffix",
            table: "AspNetUsers");
    }
}
