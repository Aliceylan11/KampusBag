using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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