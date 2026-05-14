using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly KampusBagDbContext _context;

    public UsersController(
        IUserService userService,
        ITokenService tokenService,
        KampusBagDbContext context)
    {
        _userService = userService;
        _tokenService = tokenService;
        _context = context;
    }

    // IUserService.RegisterUserAsync → string döner (mesaj veya userId)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
    {
        try
        {
            var result = await _userService.RegisterUserAsync(dto);
            return Ok(new { message = result ?? "Kayıt başarılı!" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // IUserService.VerifyEmailAsync → string döner
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string email, [FromQuery] string code)
    {
        try
        {
            var result = await _userService.VerifyEmailAsync(email, code);
            return Ok(new { message = result ?? "E-posta doğrulandı." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // Login — AuthenticateAsync User? döner (bu zaten çalışıyor)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        try
        {
            var user = await _userService.AuthenticateAsync(dto.Identifier, dto.Password);
            if (user == null)
                return Unauthorized(new { message = "Kimlik bilgileri hatalı." });
            if (!user.IsEmailVerified)
                return Unauthorized(new { message = "Lütfen önce e-postanızı doğrulayın." });

            var token = _tokenService.GenerateToken(user);

            return Ok(new
            {
                message = "Giriş başarılı!",
                token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.RegistrationNumber,
                    Role = (int)user.Role
                }
            });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // IUserService.ForgotPasswordAsync → string döner
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            var result = await _userService.ForgotPasswordAsync(dto.Email);
            return Ok(new { message = result ?? "Kod gönderildi." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // IUserService.ResetPasswordAsync → string döner
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            var result = await _userService.ResetPasswordAsync(dto.Email, dto.Code, dto.NewPassword);
            return Ok(new { message = result ?? "Şifre sıfırlandı." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // Profil — IUserService.GetByIdAsync kullanılır
    [Authorize]
    [HttpGet("profile/{userId:guid}")]
    public async Task<IActionResult> GetProfile(Guid userId)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.RegistrationNumber,
                Role = (int)user.Role,
                user.IsEmailVerified
            });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // Arama
    [Authorize]
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string term)
    {
        try
        {
            var users = await _userService.SearchUsersAsync(term);
            return Ok(users);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // FCM cihaz token'ı kaydet/güncelle
    [Authorize]
    [HttpPost("device-token")]
    public async Task<IActionResult> SaveDeviceToken([FromBody] DeviceTokenDto dto)
    {
        if (dto.UserId == Guid.Empty)
            return BadRequest(new { message = "UserId zorunludur." });
        if (string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(new { message = "Token boş olamaz." });

        try
        {
            var existing = await _context.DeviceTokens
                .FirstOrDefaultAsync(d => d.UserId == dto.UserId);

            if (existing != null)
            {
                existing.Token = dto.Token;
                existing.Platform = dto.Platform;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.DeviceTokens.Update(existing);
            }
            else
            {
                _context.DeviceTokens.Add(new DeviceToken
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.UserId,
                    Token = dto.Token,
                    Platform = dto.Platform,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cihaz token kaydedildi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Token kayıt hatası: {ex.Message}" });
        }
    }
}