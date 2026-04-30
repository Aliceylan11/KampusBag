using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
namespace KampusBag.Core.Interfaces;

public interface IUserService
{
    Task<string> RegisterUserAsync(UserRegisterDto dto);
    Task<string> VerifyEmailAsync(string email, string code); // Yeni metod
    Task<User?> AuthenticateAsync(string identifier, string password);
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
    UserRole DetermineRoleByEmail(string email);

    // YENİ METODLAR - Şifre Sıfırlama
    Task<string> ForgotPasswordAsync(string email);
    Task<string> ResetPasswordAsync(string email, string code, string newPassword);
}