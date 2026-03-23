using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KampusBag.Core.DTOs;

namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGenericRepository<User> _userRepository;

    // Hem akıllı arama için Service'i hem de genel listeleme için Repository'yi bağladık
    public UsersController(IUserService userService, IGenericRepository<User> userRepository)
    {
        _userService = userService;
        _userRepository = userRepository;
    }

    // 1. Tüm Kullanıcıları Getir (Test amaçlı)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userRepository.GetAllAsync();
        return Ok(users);
    }

    // 2. Akıllı Arama (Senin istediğin Öğrenci No / Hoca Adı ayrımı burada çalışacak)
    // URL Örneği: api/Users/search?term=2411081054
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest("Lütfen bir arama terimi girin.");

        var results = await _userService.SearchUsersAsync(term);
        return Ok(results);
    }

    // ==========================================
    // 3. YENİ EKLENEN: KAYIT OL (REGISTER) METODU
    // ==========================================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
    {
        try
        {
            // UserService'deki yazdığın mantığı çağırıyoruz
            var result = await _userService.RegisterUserAsync(registerDto);

            // Eğer mesaj "başarılı" veya "gönderildi" kelimelerini içeriyorsa işlem tamamdır
            if (result.Contains("başarılı") || result.Contains("gönderildi"))
            {
                return Ok(new { message = result }); // HTTP 200 döner
            }

            // Hata durumu varsa (örn: Mail zaten kullanımda) BadRequest dön
            return BadRequest(new { message = result }); // HTTP 400 döner
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 4. E-Posta Doğrulama
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromQuery] string email, [FromQuery] string code)
    {
        var result = await _userService.VerifyEmailAsync(email, code);

        if (result.Contains("başarıyla"))
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}