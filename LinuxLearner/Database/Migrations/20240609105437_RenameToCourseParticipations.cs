using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinuxLearner.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameToCourseParticipations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseUsers");

            migrationBuilder.CreateTable(
                name: "CourseParticipations",
                columns: table => new
                {
                    UserName = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCourseAdministrator = table.Column<bool>(type: "boolean", nullable: false),
                    JoinTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseParticipations", x => new { x.CourseId, x.UserName });
                    table.ForeignKey(
                        name: "FK_CourseParticipations_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseParticipations_Users_UserName",
                        column: x => x.UserName,
                        principalTable: "Users",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseParticipations_UserName",
                table: "CourseParticipations",
                column: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseParticipations");

            migrationBuilder.CreateTable(
                name: "CourseUsers",
                columns: table => new
                {
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    IsCourseAdministrator = table.Column<bool>(type: "boolean", nullable: false),
                    JoinTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseUsers", x => new { x.CourseId, x.UserName });
                    table.ForeignKey(
                        name: "FK_CourseUsers_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseUsers_Users_UserName",
                        column: x => x.UserName,
                        principalTable: "Users",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseUsers_UserName",
                table: "CourseUsers",
                column: "UserName");
        }
    }
}
