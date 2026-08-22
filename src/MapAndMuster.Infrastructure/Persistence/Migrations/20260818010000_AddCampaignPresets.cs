using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(CampaignDbContext))]
[Migration("20260818010000_AddCampaignPresets")]
public partial class AddCampaignPresets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CampaignPresets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                CatalogJson = table.Column<string>(type: "jsonb", nullable: true),
                SettingsJson = table.Column<string>(type: "jsonb", nullable: true),
                MapGraphJson = table.Column<string>(type: "jsonb", nullable: true),
                MapStorageKey = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignPresets", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CampaignPresets_NormalizedName",
            table: "CampaignPresets",
            column: "NormalizedName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CampaignPresets");
    }
}
