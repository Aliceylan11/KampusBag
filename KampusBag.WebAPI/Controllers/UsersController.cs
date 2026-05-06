using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IMessageService _messageService;
    private readonly IGenericRepository<Course> _courseRepository;

    public UsersController(IUserService userService, IGenericRepository<User> userRepository, 
        IMessageService messageService, IGenericRepository<Course> courseRepository)
    {
        _userService = userService;
        _userRepository = userRepository;
        _messageService = messageService;
        _courseRepository = courseRepository;
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

            // E-posta gönderimi başarılı mı kontrol et
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
            return Ok(new { message = result });
        }

        return BadRequest(new { message = result });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
        try
        {
            // Önce kullanıcının email doğrulama durumunu kontrol edelim
            var users = await _userRepository.FindAsync(u =>
                u.Email == loginDto.Identifier || u.RegistrationNumber == loginDto.Identifier);
            var userCheck = users.FirstOrDefault();

            if (userCheck != null && !userCheck.IsEmailVerified)
            {
                return Unauthorized(new
                {
                    message = "Hesabınız henüz doğrulanmamış! Lütfen e-posta adresinize gönderilen doğrulama kodunu kullanarak hesabınızı aktifleştirin.",
                    emailVerified = false
                });
            }

            // Kullanıcıyı doğrula
            var user = await _userService.AuthenticateAsync(loginDto.Identifier, loginDto.Password);

            // Doğrulama başarısız olduysa
            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Hatalı kullanıcı adı veya şifre!",
                    emailVerified = true  // Şifre hatalı, email kontrolü geçti
                });
            }

            // Başarılı giriş - Kullanıcı bilgilerini döndür
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(forgotPasswordDto.Email))
            {
                return BadRequest(new { message = "E-posta adresi gereklidir." });
            }

            var result = await _userService.ForgotPasswordAsync(forgotPasswordDto.Email);

            // Güvenlik: Her zaman aynı mesajı döndür (email enumeration saldırı önlemi)
            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Bir hata oluştu: {ex.Message}" });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resetPasswordDto.Email) ||
                string.IsNullOrWhiteSpace(resetPasswordDto.Code) ||
                string.IsNullOrWhiteSpace(resetPasswordDto.NewPassword))
            {
                return BadRequest(new { message = "Tüm alanlar gereklidir." });
            }

            // Şifre uzunluk kontrolü (minimum 6 karakter)
            if (resetPasswordDto.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });
            }

            var result = await _userService.ResetPasswordAsync(
                resetPasswordDto.Email,
                resetPasswordDto.Code,
                resetPasswordDto.NewPassword
            );

            if (result.Contains("başarıyla"))
            {
                return Ok(new { message = result });
            }

            return BadRequest(new { message = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Bir hata oluştu: {ex.Message}" });
        }
    }

    [HttpGet("profile/{id}")]
    public async Task<IActionResult> GetUserProfile(Guid id)
    {
        try
        {
            // 1. Kullanıcıyı getir
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            // 2. İstatistikleri Çek  
            var totalMessages = await _messageService.GetCountByUserIdAsync(id);

            // Kurs sayısını 'CourseMembership' tablosu üzerinden sayıyoruz
            var totalCourses = await _courseRepository.CountAsync(c => c.CourseMemberships.Any(m => m.UserId == id));
            // 3. Dinamik DTO Oluşturma
            var profileDto = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RegistrationNumber = user.RegistrationNumber, 
                Role = (int)user.Role,
                TotalCourses = totalCourses,
                TotalMessages = totalMessages
            };

            return Ok(profileDto);
        }
        catch (Exception ex)
        { 
            return BadRequest(new { message = $"Dinamik veri çekilemedi: {ex.Message}" });
        }
    }
}

