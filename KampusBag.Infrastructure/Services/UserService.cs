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
    private readonly IEmailService _emailService;

    public UserService(IGenericRepository<User> userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
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

        // 3. KRİTİK: Email doğrulanmamışsa giriş yapmasına izin verme
        if (!user.IsEmailVerified)
        {
            // Özel bir hata için null dönüyoruz, Controller'da kontrol edilecek
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

            // Kullanıcı daha önce kayıt olmuş ama doğrulamamış
            existingUser.VerificationCode = GenerateRandomCode();
            await _userRepository.UpdateAsync(existingUser);

            // DEĞİŞİKLİK: Hata yakalanmıyor, doğrudan fırlatılıyor
            await _emailService.SendVerificationCodeAsync(existingUser.Email, existingUser.VerificationCode);

            return "Doğrulama kodu tekrar gönderildi! Lütfen e-postanızı kontrol edin.";
        }

        // Yeni kullanıcı oluştur
        var verificationCode = GenerateRandomCode();

        var newUser = new User
        {
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = HashPassword(dto.Password),
            RegistrationNumber = dto.RegistrationNumber,
            Role = DetermineRoleByEmail(dto.Email),
            VerificationCode = verificationCode,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(newUser);

        // DEĞİŞİKLİK: Try-catch kaldırıldı, hata olursa Controller'a kadar çıkacak
        await _emailService.SendVerificationCodeAsync(newUser.Email, verificationCode);

        return "Kayıt başarılı! Lütfen e-posta adresinize gönderilen 6 haneli doğrulama kodunu girin.";
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
        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("E-posta adresi boş olamaz.");

        // 1. ADIM: Gizli Yönetici Kapısı (Özel Hotmail Adresin)
        // Bu kontrol en üstte olmalı ki üniversite kısıtlamasına takılmasın.
        if (email.Equals("ali_cey12@hotmail.com", StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.Admin;
        }

        // 2. ADIM: Akademisyen Kontrolü
        if (email.EndsWith("@gumushane.edu.tr", StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.Academic; // Projedeki karşılığı Teacher olarak tanımlı
        }

        // 3. ADIM: Öğrenci Kontrolü
        if (email.EndsWith("@ogr.gumushane.edu.tr", StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.Student;
        }

        // 4. ADIM: Geçersiz Mail Hatası
        throw new Exception("Geçersiz mail! Sadece Gümüşhane Üniversitesi kurumsal e-postaları kabul edilir.");
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
    {
        if (searchTerm.All(char.IsDigit))
            return await _userRepository.FindAsync(u => u.RegistrationNumber.Contains(searchTerm));

        return await _userRepository.FindAsync(u => u.FullName.ToLower().Contains(searchTerm.ToLower()));
    }

    // EKLEME YAPILACAK METODLAR - UserService.cs dosyasının SONUNA ekleyin

    // ==================== ŞİFRE SIFIRLAMA METODLARI ====================

    public async Task<string> ForgotPasswordAsync(string email)
    {
        // 1. Kullanıcıyı bul
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            // Güvenlik: Email bulunamadı bile dönmeyelim (brute force saldırı önlemi)
            return "Eğer bu e-posta sistemde kayıtlıysa, şifre sıfırlama kodu gönderildi.";
        }

        // 2. Yeni doğrulama kodu üret
        var resetCode = GenerateRandomCode();
        user.VerificationCode = resetCode;
        await _userRepository.UpdateAsync(user);

        // 3. E-posta ile kodu gönder
        await _emailService.SendPasswordResetCodeAsync(user.Email, resetCode);

        return "Eğer bu e-posta sistemde kayıtlıysa, şifre sıfırlama kodu gönderildi.";
    }

    public async Task<string> ResetPasswordAsync(string email, string code, string newPassword)
    {
        // 1. Kullanıcıyı bul
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            return "Geçersiz e-posta veya doğrulama kodu!";
        }

        // 2. Doğrulama kodunu kontrol et
        if (user.VerificationCode != code)
        {
            return "Geçersiz e-posta veya doğrulama kodu!";
        }

        // 3. Yeni şifreyi hashle ve kaydet
        user.PasswordHash = HashPassword(newPassword);
        user.VerificationCode = null; // Kodu sıfırla (tekrar kullanılmasın)
        await _userRepository.UpdateAsync(user);

        return "Şifreniz başarıyla güncellendi! Artık yeni şifrenizle giriş yapabilirsiniz.";
    }

    

}