using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapAndMuster.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSiteChatAndPreferredLanguage : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PreferredChatLanguage",
            table: "AspNetUsers",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "English");

        migrationBuilder.CreateTable(
            name: "SiteChatBlocks",
            columns: table => new
            {
                BlockerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                BlockedUserId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiteChatBlocks", x => new { x.BlockerUserId, x.BlockedUserId });
            });

        migrationBuilder.CreateTable(
            name: "SiteChatMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PostedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorUsername = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AuthorDisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                TargetUsername = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                TargetDisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiteChatMessages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SiteChatBlocks_BlockedUserId",
            table: "SiteChatBlocks",
            column: "BlockedUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SiteChatMessages_PostedUtc",
            table: "SiteChatMessages",
            column: "PostedUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SiteChatBlocks");

        migrationBuilder.DropTable(
            name: "SiteChatMessages");

        migrationBuilder.DropColumn(
            name: "PreferredChatLanguage",
            table: "AspNetUsers");
    }
}
