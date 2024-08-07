﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PinArchiverBot.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchiveChannels",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveChannels", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "BlacklistChannels",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistChannels", x => new { x.GuildId, x.ChannelId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistChannels_ChannelId",
                table: "BlacklistChannels",
                column: "ChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistChannels_GuildId",
                table: "BlacklistChannels",
                column: "GuildId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveChannels");

            migrationBuilder.DropTable(
                name: "BlacklistChannels");
        }
    }
}
