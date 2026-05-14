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
    public DbSet<DeviceToken> DeviceTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // FIX: WithMany() sonrası HasForeignKey generic olamaz
        modelBuilder.Entity<EmergencyRight>()
            .HasOne(er => er.User)
            .WithMany()
            .HasForeignKey(er => er.UserId);

        modelBuilder.Entity<EmergencyRight>()
            .HasIndex(er => new { er.UserId, er.AcademicTerm })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.RegistrationNumber)
            .IsUnique();

        modelBuilder.Entity<CourseMembership>()
            .HasIndex(cm => new { cm.CourseId, cm.UserId })
            .IsUnique();

        modelBuilder.Entity<DeviceToken>()
            .HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeviceToken>()
            .HasIndex(d => d.UserId)
            .IsUnique();
    }
}