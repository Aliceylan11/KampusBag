using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KampusBag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("40849a48-a3a9-402c-bffe-a29737ea59ca"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("a149ece9-3537-4860-bf4d-56a297e50b84"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55e6adf8-d4da-4656-bf78-e7d30a489bdb"));

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsEmailVerified", "RegistrationNumber", "Role", "VerificationCode" },
                values: new object[,]
                {
                    { new Guid("1c77627c-c02b-41e7-8a15-c2776673f661"), new DateTime(2026, 3, 15, 11, 45, 29, 728, DateTimeKind.Utc).AddTicks(9454), "2411081054@ogr.gumushane.edu.tr", "Ali Ceylan", false, "2411081054", 3, null },
                    { new Guid("3f8e35e8-bc60-446a-89e5-e81c1f35eebf"), new DateTime(2026, 3, 15, 11, 45, 29, 728, DateTimeKind.Utc).AddTicks(9426), "nihat@gumushane.edu.tr", "Nihat Özdemir", false, "SICIL-789", 2, null }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "AcademicId", "CourseCode", "Name" },
                values: new object[] { new Guid("0bf7d66d-e42e-49b9-b55f-1ec926f86fcd"), new Guid("3f8e35e8-bc60-446a-89e5-e81c1f35eebf"), "BGM301", "Mobil Programlama (.NET MAUI)" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: new Guid("0bf7d66d-e42e-49b9-b55f-1ec926f86fcd"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1c77627c-c02b-41e7-8a15-c2776673f661"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("3f8e35e8-bc60-446a-89e5-e81c1f35eebf"));

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerificationCode",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "RegistrationNumber", "Role" },
                values: new object[,]
                {
                    { new Guid("55e6adf8-d4da-4656-bf78-e7d30a489bdb"), new DateTime(2026, 3, 12, 7, 2, 55, 91, DateTimeKind.Utc).AddTicks(5000), "nihat@gumushane.edu.tr", "Nihat Özdemir", "SICIL-789", 2 },
                    { new Guid("a149ece9-3537-4860-bf4d-56a297e50b84"), new DateTime(2026, 3, 12, 7, 2, 55, 91, DateTimeKind.Utc).AddTicks(5064), "2411081054@ogr.gumushane.edu.tr", "Ali Ceylan", "2411081054", 3 }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "AcademicId", "CourseCode", "Name" },
                values: new object[] { new Guid("40849a48-a3a9-402c-bffe-a29737ea59ca"), new Guid("55e6adf8-d4da-4656-bf78-e7d30a489bdb"), "BGM301", "Mobil Programlama (.NET MAUI)" });
        }
    }
}
