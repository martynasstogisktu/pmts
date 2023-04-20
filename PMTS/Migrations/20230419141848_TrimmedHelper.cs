using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMTS.Migrations
{
    /// <inheritdoc />
    public partial class TrimmedHelper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "auth",
                table: "Helper");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "auth",
                table: "Helper",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
