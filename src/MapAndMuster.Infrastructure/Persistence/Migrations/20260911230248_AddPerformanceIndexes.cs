using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(CampaignDbContext))]
[Migration("20260911230248_AddPerformanceIndexes")]
public partial class AddPerformanceIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Pending",
            table: "OutboxMessages",
            column: "CreatedUtc",
            filter: "\"ProcessedUtc\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Campaigns_IsPubliclyViewable_StartsUtc",
            table: "Campaigns",
            columns: ["IsPubliclyViewable", "StartsUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_Campaigns_UpdatedUtc",
            table: "Campaigns",
            column: "UpdatedUtc",
            descending: []);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_Pending",
            table: "OutboxMessages");

        migrationBuilder.DropIndex(
            name: "IX_Campaigns_IsPubliclyViewable_StartsUtc",
            table: "Campaigns");

        migrationBuilder.DropIndex(
            name: "IX_Campaigns_UpdatedUtc",
            table: "Campaigns");
    }
}
