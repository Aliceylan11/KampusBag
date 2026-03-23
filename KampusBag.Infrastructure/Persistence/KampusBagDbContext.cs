using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace KampusBag.Infrastructure.Persistence;

public class KampusBagDbContext : DbContext
{
    public KampusBagDbContext(DbContextOptions<KampusBagDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseMembership> CourseMemberships { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<EmergencyRight> EmergencyRights { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Mesaj ve Gönderici İlişkisi (Silme davranışını kısıtlıyoruz ki silinen kullanıcıların eski mesajları patlamasın)
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Acil Durum Hakkı - Kullanıcı İlişkisi (Birebir ilişki)
        modelBuilder.Entity<EmergencyRight>()
            .HasOne(er => er.User)
            .WithOne()
            .HasForeignKey<EmergencyRight>(er => er.UserId);

        // 3. Öğrenci Numarası (Sicil No) Kesinlikle Tekil Olmalı
        modelBuilder.Entity<User>()
            .HasIndex(u => u.RegistrationNumber)
            .IsUnique();

        // 4. Bir Öğrenci Bir Derse Sadece Bir Kez Kaydolabilir
        modelBuilder.Entity<CourseMembership>()
            .HasIndex(cm => new { cm.CourseId, cm.UserId })
            .IsUnique();

        // Not: Başlangıç (Seed) verileri temizlendi. Artık tüm veriler uygulama üzerinden (MAUI/Swagger) eklenecek.
    }
}