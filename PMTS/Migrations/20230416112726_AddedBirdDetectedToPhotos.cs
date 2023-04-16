using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMTS.Migrations
{
    /// <inheritdoc />
    public partial class AddedBirdDetectedToPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BirdDetected",
                table: "Photo",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirdDetected",
                table: "Photo");
        }
    }
}
