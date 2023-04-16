using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMTS.Migrations
{
    /// <inheritdoc />
    public partial class TrimmedPhotoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TournamentName",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Photo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TournamentName",
                table: "Photo",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Photo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Photo",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
