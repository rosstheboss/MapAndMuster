using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(MapAndMuster.Infrastructure.Persistence.CampaignDbContext))]
[Migration("20260815010000_AddCampaignPlayState")]
public partial class AddCampaignPlayState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PlayStateJson",
            table: "Campaigns",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "FactionId",
            table: "CampaignMemberships",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Subfaction",
            table: "CampaignMemberships",
            type: "character varying(60)",
            maxLength: 60,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PlayStateJson",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "FactionId",
            table: "CampaignMemberships");

        migrationBuilder.DropColumn(
            name: "Subfaction",
            table: "CampaignMemberships");
    }
}
