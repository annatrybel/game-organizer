using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatGroupMemberTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ChatGroup_ChatGroupId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ChatGroupId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ChatGroupId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ChatGroup",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ChatGroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatGroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatGroupMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatGroupMembers_ChatGroup_ChatGroupId",
                        column: x => x.ChatGroupId,
                        principalTable: "ChatGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatGroupMembers_ChatGroupId",
                table: "ChatGroupMembers",
                column: "ChatGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatGroupMembers_UserId",
                table: "ChatGroupMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatGroupMembers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ChatGroup");

            migrationBuilder.AddColumn<int>(
                name: "ChatGroupId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ChatGroupId",
                table: "AspNetUsers",
                column: "ChatGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ChatGroup_ChatGroupId",
                table: "AspNetUsers",
                column: "ChatGroupId",
                principalTable: "ChatGroup",
                principalColumn: "Id");
        }
    }
}
