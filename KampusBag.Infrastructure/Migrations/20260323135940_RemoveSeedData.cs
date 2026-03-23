using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KampusBag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("3bfc34df-43cb-4071-9f0f-4d0bef4ef346"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("c4cbcc7d-0fa7-4cba-b07b-bc87bd19eeec"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("80499a6b-cf80-418e-86fd-21ae0865f9f7"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsEmailVerified", "PasswordHash", "RegistrationNumber", "Role", "VerificationCode" },
                values: new object[,]
                {
                    { new Guid("80499a6b-cf80-418e-86fd-21ae0865f9f7"), new DateTime(2026, 3, 23, 13, 40, 9, 99, DateTimeKind.Utc).AddTicks(218), "nihat@gumushane.edu.tr", "Nihat Özdemir", false, "hashed_password_placeholder", "SICIL-789", 2, null },
                    { new Guid("c4cbcc7d-0fa7-4cba-b07b-bc87bd19eeec"), new DateTime(2026, 3, 23, 13, 40, 9, 99, DateTimeKind.Utc).AddTicks(272), "2411081054@ogr.gumushane.edu.tr", "Ali Ceylan", false, "hashed_password_placeholder", "2411081054", 3, null }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "AcademicId", "CourseCode", "Name" },
                values: new object[] { new Guid("3bfc34df-43cb-4071-9f0f-4d0bef4ef346"), new Guid("80499a6b-cf80-418e-86fd-21ae0865f9f7"), "BGM301", "Mobil Programlama (.NET MAUI)" });
        }
    }
}
