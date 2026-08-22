using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(MapAndMuster.Infrastructure.Persistence.CampaignDbContext))]
[Migration("20260814233000_AddCampaignLocationAndPublicView")]
public partial class AddCampaignLocationAndPublicView : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "City",
            table: "Campaigns",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Country",
            table: "Campaigns",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsPubliclyViewable",
            table: "Campaigns",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "Region",
            table: "Campaigns",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "City",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "Country",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "IsPubliclyViewable",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "Region",
            table: "Campaigns");
    }
}
