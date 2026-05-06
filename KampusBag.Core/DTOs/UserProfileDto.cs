namespace KampusBag.Core.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty; // PostgreSQL'deki gerçek kolon
    public int Role { get; set; }
    public int TotalCourses { get; set; } // Dinamik hesaplanacak
    public int TotalMessages { get; set; } // Dinamik hesaplanacak
}