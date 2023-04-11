using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMTS.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedContestantAddedIDs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contestant_Tournament_TournamentId",
                table: "Contestant");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contestant_Tournament_TournamentId",
                table: "Contestant");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Contestant");

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "Contestant",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Contestant_Tournament_TournamentId",
                table: "Contestant",
                column: "TournamentId",
                principalTable: "Tournament",
                principalColumn: "Id");
        }
    }
}
