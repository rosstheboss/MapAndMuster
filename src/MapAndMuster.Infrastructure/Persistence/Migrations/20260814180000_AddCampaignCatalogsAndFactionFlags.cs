using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(MapAndMuster.Infrastructure.Persistence.CampaignDbContext))]
[Migration("20260814180000_AddCampaignCatalogsAndFactionFlags")]
public partial class AddCampaignCatalogsAndFactionFlags : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CatalogJson",
            table: "Campaigns",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Color",
            table: "CampaignFactions",
            type: "character varying(7)",
            maxLength: 7,
            nullable: false,
            defaultValue: "#2563EB");

        migrationBuilder.AddColumn<bool>(
            name: "RequiresSubfaction",
            table: "CampaignFactions",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CatalogJson",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "Color",
            table: "CampaignFactions");

        migrationBuilder.DropColumn(
            name: "RequiresSubfaction",
            table: "CampaignFactions");
    }
}
