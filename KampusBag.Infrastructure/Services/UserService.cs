using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
using KampusBag.Core.Interfaces;
using KampusBag.Core.DTOs;  
using System.Security.Cryptography;
using System.Text;

namespace KampusBag.Infrastructure.Services;

public class UserService : IUserService
{
    // 1. Önce değişkeni tanımlıyoruz
    private readonly IGenericRepository<User> _userRepository;

    // 2. Constructor (Yapıcı Metot) ile içeriye alıyoruz
    public UserService(IGenericRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }
    public async Task<string> VerifyEmailAsync(string email, string code)
    {
        // Repository üzerinden kullanıcıyı bul
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null || user.VerificationCode != code)
        {
            return "Geçersiz email veya hatalı kod!";
        }

        user.IsEmailVerified = true;
        user.VerificationCode = null; // Doğrulama bittiği için kodu temizle

        await _userRepository.UpdateAsync(user); // Repository üzerinden güncelle

        return "Hesabınız başarıyla doğrulandı. Artık giriş yapabilirsiniz!";
    }
    public async Task<string> RegisterUserAsync(UserRegisterDto dto)
    {
        // 1. Kullanıcı kontrolü
        var existingUsers = await _userRepository.FindAsync(u => u.Email == dto.Email);
        var existingUser = existingUsers.FirstOrDefault();

        if (existingUser != null)
        {
            if (existingUser.IsEmailVerified)
                return "Bu mail adresi zaten kullanımda.";

            // Onaylanmamışsa kodu yenile
            existingUser.VerificationCode = GenerateRandomCode();
            await _userRepository.UpdateAsync(existingUser);
            return "Doğrulama kodu tekrar gönderildi!";
        }

        // 2. Yeni kullanıcı (Buradaki alan isimleri User entity ile aynı olmalı)
        var newUser = new User
        {
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = HashPassword(dto.Password), // Aşağıdaki metod
            RegistrationNumber = dto.RegistrationNumber,
            Role = DetermineRoleByEmail(dto.Email),
            VerificationCode = GenerateRandomCode(),
            IsEmailVerified = false
        };

        await _userRepository.AddAsync(newUser);
        return "Kayıt başarılı! Lütfen mailinizi onaylayın.";
    }

    // Şifreleme Metodu (Hata alıyorsan bunu sınıfın içine ekle)
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