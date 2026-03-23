using System.Net.Http.Json;
using KampusBag.Core.DTOs;

namespace KampusBag.MobileUI.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    // Android Emülatör için 10.0.2.2 ve Web API portu (5178)
    private const string BaseUrl = "http://10.0.2.2:5178/api/";

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
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}users/register", registerDto);
            return response.IsSuccessStatusCode; // Eğer 200 OK dönerse true olacak
        }
        catch (Exception ex)
        {
            // İleride buraya hata loglama ekleyebiliriz
            return false;
        }
    }
}