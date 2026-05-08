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

        // 1. Mesaj ve Gönderici İlişkisi
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Acil Durum Hakkı - Kullanıcı İlişkisi
        // DÜZELTME: WithOne → WithMany (bir kullanıcının her dönem için ayrı kaydı olabilir)
        modelBuilder.Entity<EmergencyRight>()
            .HasOne(er => er.User)
            .WithMany() // Bir kullanıcının birden fazla hakkı olabilir (Bire-Çok)
            .HasForeignKey(er => er.UserId); // <EmergencyRight> kısmını sildik


        // DÜZELTME: Tek sütun unique → Bileşik unique (UserId + AcademicTerm)
        // Bu sayede "2025-Güz" ve "2026-Bahar" için ayrı kayıt tutulabilir
        modelBuilder.Entity<EmergencyRight>()
            .HasIndex(er => new { er.UserId, er.AcademicTerm })
            .IsUnique();

        // 3. Öğrenci Numarası (Sicil No) Kesinlikle Tekil Olmalı
        modelBuilder.Entity<User>()
            .HasIndex(u => u.RegistrationNumber)
            .IsUnique();

        // 4. Bir Öğrenci Bir Derse Sadece Bir Kez Kaydolabilir
        modelBuilder.Entity<CourseMembership>()
            .HasIndex(cm => new { cm.CourseId, cm.UserId })
            .IsUnique();
    }
}
