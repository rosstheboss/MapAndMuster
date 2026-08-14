using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campaign.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCampaignSchedule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "EndsUtc",
            table: "Campaigns",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        migrationBuilder.AddColumn<int>(
            name: "RoundCount",
            table: "Campaigns",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "RoundLengthAmount",
            table: "Campaigns",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "RoundLengthUnit",
            table: "Campaigns",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "StartsUtc",
            table: "Campaigns",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        migrationBuilder.AddColumn<string>(
            name: "TimeZoneId",
            table: "Campaigns",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "CampaignRoundPhases",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                DurationAmount = table.Column<int>(type: "integer", nullable: false),
                DurationUnit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignRoundPhases", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignRoundPhases_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CampaignRoundPhases_CampaignId",
            table: "CampaignRoundPhases",
            column: "CampaignId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CampaignRoundPhases");

        migrationBuilder.DropColumn(
            name: "EndsUtc",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "RoundCount",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "RoundLengthAmount",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "RoundLengthUnit",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "StartsUtc",
            table: "Campaigns");

        migrationBuilder.DropColumn(
            name: "TimeZoneId",
            table: "Campaigns");
    }
}
