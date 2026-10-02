using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFreeForAllAndRandomSpawn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "GameSystem",
            table: "Campaigns",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsFreeForAll",
            table: "Campaigns",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "RandomSpawnLocations",
            table: "Campaigns",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "PreferenceJson",
            table: "CampaignFactions",
            type: "jsonb",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GameSystem",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "IsFreeForAll",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "RandomSpawnLocations",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "PreferenceJson",
            table: "CampaignFactions");
    }
}
