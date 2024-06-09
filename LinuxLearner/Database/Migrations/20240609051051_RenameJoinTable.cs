using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinuxLearner.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseUser_Courses_CourseId",
                table: "CourseUser");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseUser_Users_UserName",
                table: "CourseUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseUser",
                table: "CourseUser");

            migrationBuilder.RenameTable(
                name: "CourseUser",
                newName: "CourseUsers");

            migrationBuilder.RenameIndex(
                name: "IX_CourseUser_UserName",
                table: "CourseUsers",
                newName: "IX_CourseUsers_UserName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseUsers",
                table: "CourseUsers",
                columns: new[] { "CourseId", "UserName" });

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUsers_Courses_CourseId",
                table: "CourseUsers",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUsers_Users_UserName",
                table: "CourseUsers",
                column: "UserName",
                principalTable: "Users",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseUsers_Courses_CourseId",
                table: "CourseUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseUsers_Users_UserName",
                table: "CourseUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseUsers",
                table: "CourseUsers");

            migrationBuilder.RenameTable(
                name: "CourseUsers",
                newName: "CourseUser");

            migrationBuilder.RenameIndex(
                name: "IX_CourseUsers_UserName",
                table: "CourseUser",
                newName: "IX_CourseUser_UserName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseUser",
                table: "CourseUser",
                columns: new[] { "CourseId", "UserName" });

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUser_Courses_CourseId",
                table: "CourseUser",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUser_Users_UserName",
                table: "CourseUser",
                column: "UserName",
                principalTable: "Users",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
