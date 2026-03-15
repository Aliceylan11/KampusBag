using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KampusBag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "Messages",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

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

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CourseId",
                table: "Messages",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Courses_CourseId",
                table: "Messages",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Courses_CourseId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_CourseId",
                table: "Messages");

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

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

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
    }
}
