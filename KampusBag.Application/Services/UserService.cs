using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
using KampusBag.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace KampusBag.Application.Services;

/// <summary>
/// #4 Onion Refactoring: UserService Application katmanına taşındı.
/// Infrastructure bağımlılığı tamamen kaldırıldı;
/// sadece Core interface'leri (IGenericRepository, IEmailService) kullanılıyor.
/// </summary>
public class UserService : IUserService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IEmailService _emailService;

    public UserService(
        IGenericRepository<User> userRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<User?> GetByIdAsync(Guid id)
        => await _userRepository.GetByIdAsync(id);

    public async Task<string> VerifyEmailAsync(string email, string code)
    {
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null || user.VerificationCode != code)
            return "Geçersiz email veya hatalı kod!";

        user.IsEmailVerified = true;
        user.VerificationCode = null;
        await _userRepository.UpdateAsync(user);

        return "Hesabınız başarıyla doğrulandı. Artık giriş yapabilirsiniz!";
    }

    public async Task<User?> AuthenticateAsync(string identifier, string password)
    {
        var users = await _userRepository.FindAsync(u =>
            u.Email == identifier || u.RegistrationNumber == identifier);
        var user = users.FirstOrDefault();

        if (user == null || !user.IsEmailVerified) return null;

        return user.PasswordHash == HashPassword(password) ? user : null;
    }

    public async Task<string> RegisterUserAsync(UserRegisterDto dto)
    {
        var existing = (await _userRepository.FindAsync(u => u.Email == dto.Email)).FirstOrDefault();

        if (existing != null)
        {
            if (existing.IsEmailVerified) return "Bu mail adresi zaten kullanımda.";

            existing.VerificationCode = GenerateCode();
            await _userRepository.UpdateAsync(existing);
            await _emailService.SendVerificationCodeAsync(existing.Email, existing.VerificationCode);
            return "Doğrulama kodu tekrar gönderildi!";
        }

        var code = GenerateCode();
        var user = new User
        {
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = HashPassword(dto.Password),
            RegistrationNumber = dto.RegistrationNumber,
            Role = DetermineRoleByEmail(dto.Email),
            VerificationCode = code,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _emailService.SendVerificationCodeAsync(user.Email, code);

        return "Kayıt başarılı! Lütfen e-posta adresinize gönderilen kodu girin.";
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
    {
        if (searchTerm.All(char.IsDigit))
            return await _userRepository.FindAsync(
                u => u.RegistrationNumber.Contains(searchTerm));

        return await _userRepository.FindAsync(
            u => u.FullName.ToLower().Contains(searchTerm.ToLower()));
    }

    public UserRole DetermineRoleByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("E-posta boş olamaz.");

        if (email.Equals("ali_cey12@hotmail.com", StringComparison.OrdinalIgnoreCase))
            return UserRole.Admin;

        if (email.EndsWith("@gumushane.edu.tr", StringComparison.OrdinalIgnoreCase))
            return UserRole.Academic;

        if (email.EndsWith("@ogr.gumushane.edu.tr", StringComparison.OrdinalIgnoreCase))
            return UserRole.Student;

        throw new Exception(
            "Geçersiz mail! Sadece Gümüşhane Üniversitesi kurumsal e-postaları kabul edilir.");
    }

    public async Task<string> ForgotPasswordAsync(string email)
    {
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        // Güvenlik: email bulunamasa da aynı mesajı döndür
        const string safeMsg = "E-posta sistemde kayıtlıysa sıfırlama kodu gönderildi.";
        if (user == null) return safeMsg;

        user.VerificationCode = GenerateCode();
        await _userRepository.UpdateAsync(user);
        await _emailService.SendPasswordResetCodeAsync(user.Email, user.VerificationCode);

        return safeMsg;
    }

    public async Task<string> ResetPasswordAsync(string email, string code, string newPassword)
    {
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null || user.VerificationCode != code)
            return "Geçersiz e-posta veya doğrulama kodu!";

        user.PasswordHash = HashPassword(newPassword);
        user.VerificationCode = null;
        await _userRepository.UpdateAsync(user);

        return "Şifreniz başarıyla güncellendi!";
    }

    // ── Yardımcılar ───────────────────────────────────────────────────
    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(
            sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private static string GenerateCode()
        => new Random().Next(100000, 999999).ToString();
}
