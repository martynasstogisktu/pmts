using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMTS.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Tournament",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournament_UserId",
                table: "Tournament",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournament_Users_UserId",
                table: "Tournament",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournament_Users_UserId",
                table: "Tournament");

            migrationBuilder.DropIndex(
                name: "IX_Tournament_UserId",
                table: "Tournament");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Tournament");
        }
    }
}
