using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Course",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Faculty",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstitutionId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Course",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Faculty",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Users");
        }
    }
}
