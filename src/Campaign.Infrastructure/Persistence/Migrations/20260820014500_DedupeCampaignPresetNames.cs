using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campaign.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(CampaignDbContext))]
[Migration("20260820014500_DedupeCampaignPresetNames")]
public partial class DedupeCampaignPresetNames : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS "IX_CampaignPresets_NormalizedName";

            UPDATE "CampaignPresets"
            SET
                "Name" = regexp_replace(btrim("Name"), '\s+', ' ', 'g'),
                "NormalizedName" = upper(regexp_replace(btrim("Name"), '\s+', ' ', 'g'));

            DELETE FROM "CampaignPresets" AS older
            WHERE EXISTS (
                SELECT 1
                FROM "CampaignPresets" AS keeper
                WHERE keeper."NormalizedName" = older."NormalizedName"
                  AND keeper."Id" <> older."Id"
                  AND (
                      keeper."UpdatedUtc" > older."UpdatedUtc"
                      OR (keeper."UpdatedUtc" = older."UpdatedUtc" AND keeper."Id" > older."Id")
                  )
            );

            CREATE UNIQUE INDEX "IX_CampaignPresets_NormalizedName"
                ON "CampaignPresets" ("NormalizedName");
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Duplicate preset rows cannot be restored.
    }
}
