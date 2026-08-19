using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Campaign.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCampaignSetup : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Campaigns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                PlayerSlotCount = table.Column<int>(type: "integer", nullable: false),
                IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                JoinPasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatorIsParticipant = table.Column<bool>(type: "boolean", nullable: false),
                MapStorageKey = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                Revision = table.Column<int>(type: "integer", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Campaigns", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CampaignAllyGroups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignAllyGroups", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignAllyGroups_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CampaignLinks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                Label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignLinks", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignLinks_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CampaignMemberships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                IsGameMaster = table.Column<bool>(type: "boolean", nullable: false),
                IsPlayer = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignMemberships", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignMemberships_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CampaignFactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                AllyGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignFactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignFactions_CampaignAllyGroups_AllyGroupId",
                    column: x => x.AllyGroupId,
                    principalTable: "CampaignAllyGroups",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_CampaignFactions_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CampaignSubfactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FactionId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignSubfactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignSubfactions_CampaignFactions_FactionId",
                    column: x => x.FactionId,
                    principalTable: "CampaignFactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CampaignAllyGroups_CampaignId",
            table: "CampaignAllyGroups",
            column: "CampaignId");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignFactions_AllyGroupId",
            table: "CampaignFactions",
            column: "AllyGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignFactions_CampaignId",
            table: "CampaignFactions",
            column: "CampaignId");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignLinks_CampaignId",
            table: "CampaignLinks",
            column: "CampaignId");

#pragma warning disable CA1861 // Generated composite index column list
        migrationBuilder.CreateIndex(
            name: "IX_CampaignMemberships_CampaignId_UserId",
            table: "CampaignMemberships",
            columns: ["CampaignId", "UserId"],
            unique: true);
#pragma warning restore CA1861

        migrationBuilder.CreateIndex(
            name: "IX_CampaignMemberships_UserId",
            table: "CampaignMemberships",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Campaigns_CreatedByUserId",
            table: "Campaigns",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignSubfactions_FactionId",
            table: "CampaignSubfactions",
            column: "FactionId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CampaignLinks");

        migrationBuilder.DropTable(
            name: "CampaignMemberships");

        migrationBuilder.DropTable(
            name: "CampaignSubfactions");

        migrationBuilder.DropTable(
            name: "CampaignFactions");

        migrationBuilder.DropTable(
            name: "CampaignAllyGroups");

        migrationBuilder.DropTable(
            name: "Campaigns");
    }
}
