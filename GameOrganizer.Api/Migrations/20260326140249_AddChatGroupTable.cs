using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatGroupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatGroupMembers_ChatGroup_ChatGroupId",
                table: "ChatGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatGroup_GroupId",
                table: "ChatMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatGroup",
                table: "ChatGroup");

            migrationBuilder.RenameTable(
                name: "ChatGroup",
                newName: "ChatGroups");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatGroups",
                table: "ChatGroups",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatGroupMembers_ChatGroups_ChatGroupId",
                table: "ChatGroupMembers",
                column: "ChatGroupId",
                principalTable: "ChatGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatGroups_GroupId",
                table: "ChatMessages",
                column: "GroupId",
                principalTable: "ChatGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatGroupMembers_ChatGroups_ChatGroupId",
                table: "ChatGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatGroups_GroupId",
                table: "ChatMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatGroups",
                table: "ChatGroups");

            migrationBuilder.RenameTable(
                name: "ChatGroups",
                newName: "ChatGroup");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatGroup",
                table: "ChatGroup",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatGroupMembers_ChatGroup_ChatGroupId",
                table: "ChatGroupMembers",
                column: "ChatGroupId",
                principalTable: "ChatGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatGroup_GroupId",
                table: "ChatMessages",
                column: "GroupId",
                principalTable: "ChatGroup",
                principalColumn: "Id");
        }
    }
}
