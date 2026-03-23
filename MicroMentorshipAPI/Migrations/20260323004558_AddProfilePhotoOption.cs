using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroMentorshipAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotoOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarMode",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoUrl",
                table: "Profiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarMode",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoUrl",
                table: "Profiles");
        }
    }
}
