namespace KampusBag.Core.DTOs;

public class UserRegisterDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
    public string RegistrationNumber { get; set; } // Öğrenci No veya Sicil No
}