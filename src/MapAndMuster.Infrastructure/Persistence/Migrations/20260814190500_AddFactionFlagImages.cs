using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(MapAndMuster.Infrastructure.Persistence.CampaignDbContext))]
[Migration("20260814190500_AddFactionFlagImages")]
public partial class AddFactionFlagImages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FlagImageStorageKey",
            table: "CampaignFactions",
            type: "character varying(260)",
            maxLength: 260,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FlagImageStorageKey",
            table: "CampaignFactions");
    }
}
