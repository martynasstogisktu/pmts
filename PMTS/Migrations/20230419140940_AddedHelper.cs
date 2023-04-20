using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMTS.Migrations
{
    /// <inheritdoc />
    public partial class AddedHelper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Helper",
                columns: table => new
                {
                    crypt = table.Column<string>(type: "text", nullable: false),
                    auth = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Helper");
        }
    }
}
