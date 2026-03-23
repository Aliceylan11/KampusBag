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
            // BaseAddress zaten tanımlı olduğu için sadece gideceği son noktayı (users/register) yazıyoruz
            var response = await _httpClient.PostAsJsonAsync("users/register", registerDto);
            return response.IsSuccessStatusCode; // Eğer 200 OK dönerse true olacak
        }
        catch (Exception ex)
        {
            // İleride buraya hata loglama ekleyebiliriz
            return false;
        }
    }
}