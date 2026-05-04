using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KampusBag.Infrastructure.Migrations
{
    public partial class UpdateMessageFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IsRead: Okunmamış mesaj sayısı için
            // Default false → yeni mesajlar okunmamış başlar
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // IsSilent: Sessiz mod kaydı için
            // Default false → normal mesajlar
            migrationBuilder.AddColumn<bool>(
                name: "IsSilent",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Performans için kritik index:
            // Sohbet listesi sorgusunda (SenderId, ReceiverId, SentAt) üçlüsü çok sık kullanılıyor
            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId_ReceiverId_SentAt",
                table: "Messages",
                columns: new[] { "SenderId", "ReceiverId", "SentAt" });

            // Okunmamış sayısı sorgusu için
            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId_IsRead",
                table: "Messages",
                columns: new[] { "ReceiverId", "IsRead" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ReceiverId_IsRead",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SenderId_ReceiverId_SentAt",
                table: "Messages");

            migrationBuilder.DropColumn(name: "IsRead", table: "Messages");
            migrationBuilder.DropColumn(name: "IsSilent", table: "Messages");
        }
    }
}
