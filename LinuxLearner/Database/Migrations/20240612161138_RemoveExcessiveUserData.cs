using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinuxLearner.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExcessiveUserData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseParticipations_Users_UserName",
                table: "CourseParticipations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseParticipations",
                table: "CourseParticipations");

            migrationBuilder.DropIndex(
                name: "IX_CourseParticipations_UserName",
                table: "CourseParticipations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RegistrationTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "CourseParticipations");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "CourseParticipations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseParticipations",
                table: "CourseParticipations",
                columns: new[] { "CourseId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseParticipations_UserId",
                table: "CourseParticipations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseParticipations_Users_UserId",
                table: "CourseParticipations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseParticipations_Users_UserId",
                table: "CourseParticipations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseParticipations",
                table: "CourseParticipations");

            migrationBuilder.DropIndex(
                name: "IX_CourseParticipations_UserId",
                table: "CourseParticipations");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CourseParticipations");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegistrationTime",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "CourseParticipations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseParticipations",
                table: "CourseParticipations",
                columns: new[] { "CourseId", "UserName" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseParticipations_UserName",
                table: "CourseParticipations",
                column: "UserName");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseParticipations_Users_UserName",
                table: "CourseParticipations",
                column: "UserName",
                principalTable: "Users",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
