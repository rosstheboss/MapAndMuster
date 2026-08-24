using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(CampaignDbContext))]
[Migration("20260824180000_AddDateTimeDisplayFormat")]
public partial class AddDateTimeDisplayFormat : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DateTimeDisplayFormat",
            table: "AspNetUsers",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "MonthDayYear12h");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DateTimeDisplayFormat",
            table: "AspNetUsers");
    }
}
