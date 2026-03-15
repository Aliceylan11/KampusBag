using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
namespace KampusBag.Core.Interfaces;

public interface IUserService
{
    Task<string> RegisterUserAsync(UserRegisterDto dto);
    Task<string> VerifyEmailAsync(string email, string code); // Yeni metod
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
    UserRole DetermineRoleByEmail(string email);
}