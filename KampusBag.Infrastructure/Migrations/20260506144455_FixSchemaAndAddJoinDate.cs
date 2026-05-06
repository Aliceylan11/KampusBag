using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KampusBag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSchemaAndAddJoinDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hata veren IsRead kısmı buradan temizlendi.

            

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinDate",
                table: "CourseMemberships",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsSilent",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "JoinDate",
                table: "CourseMemberships");
        }
    }
}
