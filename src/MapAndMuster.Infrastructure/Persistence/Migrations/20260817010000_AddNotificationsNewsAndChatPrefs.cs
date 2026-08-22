using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(CampaignDbContext))]
[Migration("20260817010000_AddNotificationsNewsAndChatPrefs")]
public partial class AddNotificationsNewsAndChatPrefs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EmailNotificationsEnabled",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "InAppNotificationsEnabled",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "NewsArticles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                BodyMarkdown = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                PublishedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NewsArticles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserNotifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                CampaignName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Path = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                DedupeKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ReadUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserNotifications", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NewsArticles_PublishedUtc",
            table: "NewsArticles",
            column: "PublishedUtc");

        migrationBuilder.CreateIndex(
            name: "IX_UserNotifications_UserId_DedupeKey",
            table: "UserNotifications",
            columns: ["UserId", "DedupeKey"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserNotifications_UserId_ReadUtc_CreatedUtc",
            table: "UserNotifications",
            columns: ["UserId", "ReadUtc", "CreatedUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NewsArticles");
        migrationBuilder.DropTable(name: "UserNotifications");
        migrationBuilder.DropColumn(name: "EmailNotificationsEnabled", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "InAppNotificationsEnabled", table: "AspNetUsers");
    }
}
