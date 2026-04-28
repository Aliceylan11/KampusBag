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

    public UsersController(IUserService userService, IGenericRepository<User> userRepository)
    {
        _userService = userService;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userRepository.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest("Lütfen bir arama terimi girin.");

        var results = await _userService.SearchUsersAsync(term);
        return Ok(results);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
    {
        try
        {
            var result = await _userService.RegisterUserAsync(registerDto);

            if (result.Contains("başarılı") || result.Contains("gönderildi"))
            {
                return Ok(new { message = result });
            }

            return BadRequest(new { message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
        try
        {
            // 1. Kullanıcıyı doğrula
            var user = await _userService.AuthenticateAsync(loginDto.Identifier, loginDto.Password);

            // 2. Doğrulama başarısız olduysa
            if (user == null)
            {
                return Unauthorized(new { message = "Hatalı kullanıcı adı, şifre veya hesabınız doğrulanmamış!" });
            }

            // 3. Başarılı giriş - Kullanıcı bilgilerini döndür
            return Ok(new
            {
                message = "Giriş başarılı!",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.RegistrationNumber,
                    user.Role
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Giriş sırasında bir hata oluştu: {ex.Message}" });
        }
    }
}