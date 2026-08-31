using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(CampaignDbContext))]
[Migration("20260831160000_AddSubfactionAppearance")]
public partial class AddSubfactionAppearance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Color",
            table: "CampaignSubfactions",
            type: "character varying(7)",
            maxLength: 7,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FlagSource",
            table: "CampaignSubfactions",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "inherit");

        migrationBuilder.AddColumn<string>(
            name: "FlagImageStorageKey",
            table: "CampaignSubfactions",
            type: "character varying(260)",
            maxLength: 260,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "TintFlagImage",
            table: "CampaignSubfactions",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Color",
            table: "CampaignSubfactions");

        migrationBuilder.DropColumn(
            name: "FlagSource",
            table: "CampaignSubfactions");

        migrationBuilder.DropColumn(
            name: "FlagImageStorageKey",
            table: "CampaignSubfactions");

        migrationBuilder.DropColumn(
            name: "TintFlagImage",
            table: "CampaignSubfactions");
    }
}
