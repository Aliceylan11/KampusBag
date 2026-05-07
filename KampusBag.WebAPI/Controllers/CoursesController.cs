using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using KampusBag.Core.DTOs;
using KampusBag.Core.Enums;

namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly IGenericRepository<Course> _courseRepository;
    private readonly IGenericRepository<CourseMembership> _membershipRepository;
    private readonly IGenericRepository<User> _userRepository;
    public CoursesController(
        IGenericRepository<Course> courseRepository,
        IGenericRepository<CourseMembership> membershipRepository,
        IGenericRepository<User> userRepository)
    {
        _courseRepository = courseRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCourse([FromBody] CourseCreateDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(dto.AcademicId);
            // 1. Yeni ders nesnesini oluştur
            var newCourse = new Course
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                CourseCode = dto.CourseCode.ToUpper(),
                AcademicId = dto.AcademicId,
                IsOfficial = user != null && user.Role == (UserRole)2 // Sadece rolü 2 (Hoca) olan kullanıcılar resmi ders oluşturabilir

            };

            // 2. Veritabanına kaydet
            await _courseRepository.AddAsync(newCourse);

            // 3. Dersi oluşturan hocayı otomatik olarak derse üye yap (Rol 2 = Hoca)
            var membership = new CourseMembership
            {
                Id = Guid.NewGuid(),
                CourseId = newCourse.Id,
                UserId = dto.AcademicId,
                JoinDate = DateTime.UtcNow
            };
            await _membershipRepository.AddAsync(membership);

            // 4. Mobil uygulamanın beklediği JSON formatında dön
            return Ok(new
            {
                message = "Ders başarıyla oluşturuldu.",
                courseId = newCourse.Id
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Ders oluşturulurken hata: {ex.Message}" });
        }
    }
    [HttpPost("join")]
    public async Task<IActionResult> JoinCourse([FromBody] JoinCourseDto dto)
    {
        try
        {
            // 1. Dersi kodundan bul 
            var courses = await _courseRepository.FindAsync(c => c.CourseCode == dto.CourseCode.ToUpper());
            var course = courses.FirstOrDefault();
            if (course == null)
                return NotFound(new { message = "Bu koda sahip bir ders bulunamadı." });

            // 2. Kullanıcıyı derse ekle
            var membership = new CourseMembership
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                UserId = dto.UserId,
                JoinDate = DateTime.UtcNow,
                IsRepresentative = false
            };

            await _membershipRepository.AddAsync(membership);

            return Ok(new { message = "Derse başarıyla katıldınız!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Derse katılırken hata oluştu: {ex.Message}" });
        }
    }

    // Dosyanın en altındaki DTO sınıflarının arasına bunu da ekle:
    public class JoinCourseDto
    {
        public string CourseCode { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }
}
