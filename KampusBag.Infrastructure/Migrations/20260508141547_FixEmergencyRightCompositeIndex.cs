using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KampusBag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEmergencyRightCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmergencyRights_UserId",
                table: "EmergencyRights");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRights_UserId_AcademicTerm",
                table: "EmergencyRights",
                columns: new[] { "UserId", "AcademicTerm" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmergencyRights_UserId_AcademicTerm",
                table: "EmergencyRights");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyRights_UserId",
                table: "EmergencyRights",
                column: "UserId",
                unique: true);
        }
    }
}
