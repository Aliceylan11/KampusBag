using System.Net.Http.Json;
using KampusBag.Core.DTOs;

namespace KampusBag.MobileUI.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    // Platforma göre API adresini otomatik belirleyen sihirli blok:
#if ANDROID
    // Android Emülatör için özel IP
    private const string BaseUrl = "http://10.0.2.2:5178/api/";
#else
    // Windows Machine, iOS ve Mac için standart Localhost
    private const string BaseUrl = "http://localhost:5178/api/";
#endif

    // YENİ EKLEME: Basit Session yönetimi (static)
    public static class Session
    {
        public static Guid UserId { get; set; }
        public static string Email { get; set; }
        public static string FullName { get; set; }
        public static int Role { get; set; }
        public static bool IsLoggedIn { get; set; }

        public static void Clear()
        {
            UserId = Guid.Empty;
            Email = string.Empty;
            FullName = string.Empty;
            Role = 0;
            IsLoggedIn = false;
        }
    }

    public ApiService()
    {
        // Adresi elle yazmak yerine yukarıdaki BaseUrl değişkenini çekiyoruz
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    // 1. KAYIT OLMA METODU
    public async Task<bool> RegisterAsync(UserRegisterDto registerDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("users/register", registerDto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    // 2. EMAIL DOĞRULAMA METODU
    public async Task<string> VerifyEmailAsync(string email, string code)
    {
        try
        {
            var response = await _httpClient.PostAsync($"users/verify?email={email}&code={code}", null);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }
        catch (Exception ex)
        {
            return $"Hata: {ex.Message}";
        }
    }

    // 3. YENİ EKLEME: LOGIN METODU
    public async Task<(bool success, string message)> LoginAsync(string identifier, string password)
    {
        try
        {
            // Login DTO'sunu hazırlıyoruz
            var loginDto = new UserLoginDto
            {
                Identifier = identifier,
                Password = password
            };

            // Backend'e POST isteği gönderiyoruz
            var response = await _httpClient.PostAsJsonAsync("users/login", loginDto);

            // Response içeriğini okuyoruz
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Başarılı giriş - JSON'ı parse ediyoruz
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (loginResponse?.User != null)
                {
                    // Session'a kullanıcı bilgilerini kaydediyoruz
                    Session.UserId = loginResponse.User.Id;
                    Session.Email = loginResponse.User.Email;
                    Session.FullName = loginResponse.User.FullName;
                    Session.Role = loginResponse.User.Role;
                    Session.IsLoggedIn = true;

                    return (true, loginResponse.Message ?? "Giriş başarılı!");
                }

                return (false, "Kullanıcı bilgileri alınamadı");
            }
            else
            {
                // Hatalı giriş - Backend'den gelen error mesajını parse et
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    return (false, errorResponse?.Message ?? "Giriş başarısız");
                }
                catch
                {
                    return (false, "Hatalı kullanıcı adı veya şifre!");
                }
            }
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}");
        }
    }
    // EKLEME YAPILACAK METODLAR - ApiService.cs dosyasına ekleyin

    // 4. ŞİFREMİ UNUTTUM - KOD GÖNDERME
    public async Task<(bool success, string message)> ForgotPasswordAsync(string email)
    {
        try
        {
            var forgotPasswordDto = new { Email = email };

            var response = await _httpClient.PostAsJsonAsync("users/forgot-password", forgotPasswordDto);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
                return (true, result?.Message ?? "Şifre sıfırlama kodu e-posta adresinize gönderildi.");
            }

            try
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, errorResponse?.Message ?? "Bir hata oluştu");
            }
            catch
            {
                return (false, "E-posta gönderiminde bir sorun oluştu");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}");
        }
    }

    // 5. ŞİFRE SIFIRLAMA - YENİ ŞİFRE KAYDETME
    public async Task<(bool success, string message)> ResetPasswordAsync(string email, string code, string newPassword)
    {
        try
        {
            var resetPasswordDto = new
            {
                Email = email,
                Code = code,
                NewPassword = newPassword
            };

            var response = await _httpClient.PostAsJsonAsync("users/reset-password", resetPasswordDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
                return (true, result?.Message ?? "Şifreniz başarıyla güncellendi!");
            }

            try
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, errorResponse?.Message ?? "Şifre sıfırlama başarısız");
            }
            catch
            {
                return (false, "Geçersiz kod veya e-posta");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}");
        }
    }

    // YENİ HELPER CLASS (Backend response modelleri bölümüne ekleyin)
    private class MessageResponse
    {
        public string Message { get; set; }
    }
    // Backend response modelleri
    private class LoginResponse
    {
        public string Message { get; set; }
        public UserInfo User { get; set; }
    }
    private class UserInfo
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string RegistrationNumber { get; set; }
        public int Role { get; set; }
    }
    private class ErrorResponse
    {
        public string Message { get; set; }
    }


}