using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMTS.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedContestant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contestant_Tournament_TournamentId",
                table: "Contestant");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Contestant");

            migrationBuilder.AlterColumn<string>(
                name: "Organizer",
                table: "Tournament",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "Contestant",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "TournamentName",
                table: "Contestant",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Contestant",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Contestant_Tournament_TournamentId",
                table: "Contestant",
                column: "TournamentId",
                principalTable: "Tournament",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contestant_Tournament_TournamentId",
                table: "Contestant");

            migrationBuilder.DropColumn(
                name: "TournamentName",
                table: "Contestant");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Contestant");

            migrationBuilder.AlterColumn<string>(
                name: "Organizer",
                table: "Tournament",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "Contestant",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Contestant",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Contestant_Tournament_TournamentId",
                table: "Contestant",
                column: "TournamentId",
                principalTable: "Tournament",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
