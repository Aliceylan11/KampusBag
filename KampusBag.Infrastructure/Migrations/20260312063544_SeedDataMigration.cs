using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KampusBag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StudentNo",
                table: "Users",
                newName: "RegistrationNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Users_StudentNo",
                table: "Users",
                newName: "IX_Users_RegistrationNumber");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "RegistrationNumber", "Role" },
                values: new object[,]
                {
                    { new Guid("907926f4-c384-4a96-9f1e-d5b6da5cee62"), new DateTime(2026, 3, 12, 6, 35, 43, 967, DateTimeKind.Utc).AddTicks(528), "2411081054@ogr.gumushane.edu.tr", "Ali Ceylan", "2411081054", 3 },
                    { new Guid("fcbe8ad4-0d0f-4a2f-9a41-648c0b4368d4"), new DateTime(2026, 3, 12, 6, 35, 43, 967, DateTimeKind.Utc).AddTicks(500), "nihat@gumushane.edu.tr", "Nihat Özdemir", "SICIL-789", 2 }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "AcademicId", "CourseCode", "Name" },
                values: new object[] { new Guid("51e74f21-cb4d-4854-9a13-b6e3cb65d5c4"), new Guid("fcbe8ad4-0d0f-4a2f-9a41-648c0b4368d4"), "BGM301", "Mobil Programlama (.NET MAUI)" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("51e74f21-cb4d-4854-9a13-b6e3cb65d5c4"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("907926f4-c384-4a96-9f1e-d5b6da5cee62"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("fcbe8ad4-0d0f-4a2f-9a41-648c0b4368d4"));

            migrationBuilder.RenameColumn(
                name: "RegistrationNumber",
                table: "Users",
                newName: "StudentNo");

            migrationBuilder.RenameIndex(
                name: "IX_Users_RegistrationNumber",
                table: "Users",
                newName: "IX_Users_StudentNo");
        }
    }
}
