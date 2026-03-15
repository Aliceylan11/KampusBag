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
        // İlişkileri ve kısıtlamaları burada belirleyeceğiz (Fluent API)
        modelBuilder.Entity<Message>().HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);

        // 2. Acil Durum Hakkı - Kullanıcı İlişkisi
        modelBuilder.Entity<EmergencyRight>().HasOne(er => er.User).WithOne().HasForeignKey<EmergencyRight>(er => er.UserId); // Her öğrencinin o dönem bir hak seti olur

        // 3. Öğrenci No Tekil Olmalı (Mahremiyet ve Arama için)
        modelBuilder.Entity<User>().HasIndex(u => u.RegistrationNumber).IsUnique();

        // 4. Ders Temsilcisi Belirleme
        modelBuilder.Entity<CourseMembership>().HasIndex(cm => new { cm.CourseId, cm.UserId }).IsUnique();
        var academicId = Guid.NewGuid();
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = academicId,
            FullName = "Nihat Özdemir",
            RegistrationNumber = "SICIL-789", // Hocanın akademik kimliği
            Email = "nihat@gumushane.edu.tr",
            Role = UserRole.Academic,
            CreatedAt = DateTime.UtcNow
        });
        // 2. Örnek Temsilci (Senin profilin)
        var studentId = Guid.NewGuid();
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = studentId,
            FullName = "Ali Ceylan",
            RegistrationNumber = "2411081054",
            Email = "2411081054@ogr.gumushane.edu.tr",
            Role = UserRole.Representative,
            CreatedAt = DateTime.UtcNow
        });

        // 3. Örnek Ders
        var courseId = Guid.NewGuid();
        modelBuilder.Entity<Course>().HasData(new Course
        {
            Id = courseId,
            Name = "Mobil Programlama (.NET MAUI)",
            CourseCode = "BGM301",
            AcademicId = academicId
        });
    }
}