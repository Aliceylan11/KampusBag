using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
using KampusBag.Core.Interfaces;
using KampusBag.Core.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace KampusBag.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IGenericRepository<User> _userRepository;

    public UserService(IGenericRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<string> VerifyEmailAsync(string email, string code)
    {
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null || user.VerificationCode != code)
        {
            return "Geçersiz email veya hatalı kod!";
        }

        user.IsEmailVerified = true;
        user.VerificationCode = null;

        await _userRepository.UpdateAsync(user);

        return "Hesabınız başarıyla doğrulandı. Artık giriş yapabilirsiniz!";
    }

    public async Task<User?> AuthenticateAsync(string identifier, string password)
    {
        // 1. Identifier ile kullanıcıyı bul (Email veya RegistrationNumber olabilir)
        var users = await _userRepository.FindAsync(u =>
            u.Email == identifier || u.RegistrationNumber == identifier);
        var user = users.FirstOrDefault();

        // 2. Kullanıcı bulunamadıysa null dön
        if (user == null)
        {
            return null;
        }

        // 3. Email doğrulanmamışsa giriş yapmasına izin verme
        if (!user.IsEmailVerified)
        {
            return null;
        }

        // 4. Şifre kontrolü (Şifreyi hash'le ve veritabanındaki ile karşılaştır)
        string hashedPassword = HashPassword(password);

        if (user.PasswordHash != hashedPassword)
        {
            return null;
        }

        // 5. Her şey doğruysa kullanıcıyı döndür
        return user;
    }

    public async Task<string> RegisterUserAsync(UserRegisterDto dto)
    {
        var existingUsers = await _userRepository.FindAsync(u => u.Email == dto.Email);
        var existingUser = existingUsers.FirstOrDefault();

        if (existingUser != null)
        {
            if (existingUser.IsEmailVerified)
                return "Bu mail adresi zaten kullanımda.";

            existingUser.VerificationCode = GenerateRandomCode();
            await _userRepository.UpdateAsync(existingUser);
            return "Doğrulama kodu tekrar gönderildi!";
        }

        var newUser = new User
        {
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = HashPassword(dto.Password),
            RegistrationNumber = dto.RegistrationNumber,
            Role = DetermineRoleByEmail(dto.Email),
            VerificationCode = GenerateRandomCode(),
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var task = _userRepository.AddAsync(newUser);
        await task;

        return "Kayıt başarılı! Lütfen mailinizi onaylayın.";
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GenerateRandomCode() => new Random().Next(100000, 999999).ToString();

    public UserRole DetermineRoleByEmail(string email)
    {
        if (email.EndsWith("@ogr.gumushane.edu.tr")) return UserRole.Student;
        if (email.EndsWith("@gumushane.edu.tr")) return UserRole.Academic;
        throw new Exception("Geçersiz mail!");
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
    {
        if (searchTerm.All(char.IsDigit))
            return await _userRepository.FindAsync(u => u.RegistrationNumber.Contains(searchTerm));

        return await _userRepository.FindAsync(u => u.FullName.ToLower().Contains(searchTerm.ToLower()));
    }
}