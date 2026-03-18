using System.Net.Http.Json;
using KampusBag.Core.DTOs;

namespace KampusBag.MobileUI.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    // Şimdilik Windows Machine'de (Bilgisayarda) test edeceğimiz için localhost kullanıyoruz.
    // Eğer portun farklıysa (örneğin 5001 veya 7000), burayı ona göre değiştir.
    private const string BaseUrl = "http://localhost:5433/api/";
    public ApiService()
    {
        _httpClient = new HttpClient();
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