using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VietStart.API.Migrations
{
    /// <inheritdoc />
    public partial class updateStartup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Traction",
                table: "StartUps",
                newName: "Plan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Plan",
                table: "StartUps",
                newName: "Traction");
        }
    }
}
