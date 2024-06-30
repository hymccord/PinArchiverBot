using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinArchiverBot.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class blacklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlacklistChannels",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistChannels", x => x.GuildId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistChannels_ChannelId",
                table: "BlacklistChannels",
                column: "ChannelId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlacklistChannels");
        }
    }
}
