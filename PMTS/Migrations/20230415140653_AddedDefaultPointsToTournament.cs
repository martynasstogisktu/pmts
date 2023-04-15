using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PMTS.Migrations
{
    /// <inheritdoc />
    public partial class AddedDefaultPointsToTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bird_Tournament_TournamentId",
                table: "Bird");

            migrationBuilder.AddColumn<int>(
                name: "DefaultPoints",
                table: "Tournament",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "Bird",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TournamentName",
                table: "Bird",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Photo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ContestantId = table.Column<int>(type: "integer", nullable: false),
                    TournamentName = table.Column<string>(type: "text", nullable: false),
                    TournamentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photo_Contestant_ContestantId",
                        column: x => x.ContestantId,
                        principalTable: "Contestant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Photo_ContestantId",
                table: "Photo",
                column: "ContestantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bird_Tournament_TournamentId",
                table: "Bird",
                column: "TournamentId",
                principalTable: "Tournament",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bird_Tournament_TournamentId",
                table: "Bird");

            migrationBuilder.DropTable(
                name: "Photo");

            migrationBuilder.DropColumn(
                name: "DefaultPoints",
                table: "Tournament");

            migrationBuilder.DropColumn(
                name: "TournamentName",
                table: "Bird");

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "Bird",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Bird_Tournament_TournamentId",
                table: "Bird",
                column: "TournamentId",
                principalTable: "Tournament",
                principalColumn: "Id");
        }
    }
}
