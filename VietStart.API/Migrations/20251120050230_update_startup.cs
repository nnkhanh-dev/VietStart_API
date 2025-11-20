using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VietStart.API.Migrations
{
    /// <inheritdoc />
    public partial class update_startup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdeaPoint",
                table: "StartUps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlanPoint",
                table: "StartUps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrototypePoint",
                table: "StartUps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelationshipPoint",
                table: "StartUps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamPoint",
                table: "StartUps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdeaPoint",
                table: "StartUps");

            migrationBuilder.DropColumn(
                name: "PlanPoint",
                table: "StartUps");

            migrationBuilder.DropColumn(
                name: "PrototypePoint",
                table: "StartUps");

            migrationBuilder.DropColumn(
                name: "RelationshipPoint",
                table: "StartUps");

            migrationBuilder.DropColumn(
                name: "TeamPoint",
                table: "StartUps");
        }
    }
}
