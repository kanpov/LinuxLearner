using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinuxLearner.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseDiscoverability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Discoverable",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discoverable",
                table: "Courses");
        }
    }
}
