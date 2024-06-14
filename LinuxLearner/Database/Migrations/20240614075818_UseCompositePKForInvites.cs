using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinuxLearner.Database.Migrations
{
    /// <inheritdoc />
    public partial class UseCompositePKForInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseInvites",
                table: "CourseInvites");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseInvites",
                table: "CourseInvites",
                columns: new[] { "Id", "CourseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseInvites",
                table: "CourseInvites");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseInvites",
                table: "CourseInvites",
                column: "Id");
        }
    }
}
