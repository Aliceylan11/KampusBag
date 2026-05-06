using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using KampusBag.Core.DTOs;
namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly IGenericRepository<Course> _courseRepository;
    private readonly IGenericRepository<CourseMembership> _membershipRepository;

    public CoursesController(
        IGenericRepository<Course> courseRepository,
        IGenericRepository<CourseMembership> membershipRepository)
    {
        _courseRepository = courseRepository;
        _membershipRepository = membershipRepository;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCourse([FromBody] CourseCreateDto dto)
    {
        try
        {
            // 1. Yeni ders nesnesini oluştur
            var newCourse = new Course
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                CourseCode = dto.CourseCode.ToUpper()
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
}
