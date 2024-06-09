using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinuxLearner.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddExplicitJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseUser_Courses_CoursesId",
                table: "CourseUser");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseUser_Users_UsersName",
                table: "CourseUser");

            migrationBuilder.RenameColumn(
                name: "UsersName",
                table: "CourseUser",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "CoursesId",
                table: "CourseUser",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseUser_UsersName",
                table: "CourseUser",
                newName: "IX_CourseUser_UserName");

            migrationBuilder.AddColumn<bool>(
                name: "IsCourseAdministrator",
                table: "CourseUser",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "JoinTime",
                table: "CourseUser",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseUser_Courses_CourseId",
                table: "CourseUser");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseUser_Users_UserName",
                table: "CourseUser");

            migrationBuilder.DropColumn(
                name: "IsCourseAdministrator",
                table: "CourseUser");

            migrationBuilder.DropColumn(
                name: "JoinTime",
                table: "CourseUser");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "CourseUser",
                newName: "UsersName");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "CourseUser",
                newName: "CoursesId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseUser_UserName",
                table: "CourseUser",
                newName: "IX_CourseUser_UsersName");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUser_Courses_CoursesId",
                table: "CourseUser",
                column: "CoursesId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseUser_Users_UsersName",
                table: "CourseUser",
                column: "UsersName",
                principalTable: "Users",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
