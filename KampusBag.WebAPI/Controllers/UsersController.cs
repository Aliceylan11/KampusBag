using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IMessageService _messageService;
    private readonly IGenericRepository<Course> _courseRepository;
    private readonly ITokenService _tokenService;

    public UsersController(
        IUserService userService,
        IGenericRepository<User> userRepository,
        IMessageService messageService,
        IGenericRepository<Course> courseRepository,
        ITokenService tokenService)
    {
        _userService = userService;
        _userRepository = userRepository;
        _messageService = messageService;
        _courseRepository = courseRepository;
        _tokenService = tokenService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _userRepository.GetAllAsync());

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest("Arama terimi gereklidir.");
        return Ok(await _userService.SearchUsersAsync(term));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
    {
        try
        {
            var result = await _userService.RegisterUserAsync(dto);
            return (result.Contains("başarılı") || result.Contains("gönderildi"))
                ? Ok(new { message = result })
                : BadRequest(new { message = result });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromQuery] string email, [FromQuery] string code)
    {
        var result = await _userService.VerifyEmailAsync(email, code);
        return result.Contains("başarıyla")
            ? Ok(new { message = result })
            : BadRequest(new { message = result });
    }

    // ─────────────────────────────────────────────────────────────────
    // #2 JWT: Başarılı girişte token üretip döndürür.
    // Mobile bu token'ı Session.Token'a kaydeder ve
    // sonraki tüm isteklerde Authorization: Bearer {token} gönderir.
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
        try
        {
            var users = await _userRepository.FindAsync(u =>
                u.Email == loginDto.Identifier || u.RegistrationNumber == loginDto.Identifier);
            var userCheck = users.FirstOrDefault();

            if (userCheck != null && !userCheck.IsEmailVerified)
                return Unauthorized(new { message = "E-posta doğrulanmamış!", emailVerified = false });

            var user = await _userService.AuthenticateAsync(loginDto.Identifier, loginDto.Password);
            if (user == null)
                return Unauthorized(new { message = "Hatalı kullanıcı adı veya şifre!", emailVerified = true });

            return Ok(new
            {
                message = "Giriş başarılı!",
                token = _tokenService.GenerateToken(user),  // ← JWT
                user = new { user.Id, user.Email, user.FullName, user.RegistrationNumber, user.Role }
            });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "E-posta gereklidir." });
            return Ok(new { message = await _userService.ForgotPasswordAsync(dto.Email) });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Code) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Tüm alanlar gereklidir." });

            if (dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });

            var result = await _userService.ResetPasswordAsync(dto.Email, dto.Code, dto.NewPassword);
            return result.Contains("başarıyla")
                ? Ok(new { message = result })
                : BadRequest(new { message = result });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("profile/{id}")]
    public async Task<IActionResult> GetUserProfile(Guid id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RegistrationNumber = user.RegistrationNumber,
                Role = (int)user.Role,
                TotalCourses = await _courseRepository
                    .CountAsync(c => c.CourseMemberships.Any(m => m.UserId == id)),
                TotalMessages = await _messageService.GetCountByUserIdAsync(id)
            });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
