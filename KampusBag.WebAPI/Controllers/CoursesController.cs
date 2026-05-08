using KampusBag.Core.DTOs;
using KampusBag.Core.Entities;
using KampusBag.Core.Enums;
using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KampusBag.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly IGenericRepository<Course> _courseRepository;
    private readonly IGenericRepository<CourseMembership> _membershipRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly KampusBagDbContext _context;

    public CoursesController(
        IGenericRepository<Course> courseRepository,
        IGenericRepository<CourseMembership> membershipRepository,
        IGenericRepository<User> userRepository,
        KampusBagDbContext context)
    {
        _courseRepository = courseRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _context = context;
    }

    // ════════════════════════════════════════════════════════════════════
    // POST api/courses/create
    // Ders / çalışma odası oluştur
    // ════════════════════════════════════════════════════════════════════
    [HttpPost("create")]
    public async Task<IActionResult> CreateCourse([FromBody] CourseCreateDto dto)
    {
        try
        {
            if (dto.AcademicId == Guid.Empty)
                return BadRequest(new { message = "AcademicId zorunludur." });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Ders adı zorunludur." });

            if (string.IsNullOrWhiteSpace(dto.CourseCode) || dto.CourseCode.Length != 6)
                return BadRequest(new { message = "Katılım kodu tam 6 karakter olmalıdır." });

            // Aynı koda sahip başka ders var mı?
            var existing = await _courseRepository
                .FindAsync(c => c.CourseCode == dto.CourseCode.ToUpper());
            if (existing.Any())
                return Conflict(new { message = "Bu katılım kodu zaten kullanımda. Farklı bir kod seçin." });

            var creator = await _userRepository.GetByIdAsync(dto.AcademicId);
            if (creator == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            // IsOfficial: Akademisyen (Role=2) veya Admin (Role=4) oluşturursa resmi kanal
            bool isOfficial = creator.Role is UserRole.Academic or UserRole.Admin;

            var newCourse = new Course
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                CourseCode = dto.CourseCode.ToUpper(),
                AcademicId = dto.AcademicId,   // DÜZELTME: Önceki kodda bu satır eksikti
                IsOfficial = isOfficial
            };

            await _courseRepository.AddAsync(newCourse);

            // Dersi oluşturan kullanıcıyı otomatik üye yap
            var membership = new CourseMembership
            {
                Id = Guid.NewGuid(),
                CourseId = newCourse.Id,
                UserId = dto.AcademicId,
                IsRepresentative = false,
                JoinDate = DateTime.UtcNow
            };
            await _membershipRepository.AddAsync(membership);

            return Ok(new
            {
                message = isOfficial
                    ? "Resmi kanal başarıyla oluşturuldu."
                    : "Çalışma odası başarıyla oluşturuldu.",
                courseId = newCourse.Id,
                isOfficial
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Ders oluşturulurken hata: {ex.Message}" });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // POST api/courses/join
    // Derse katıl (kod ile)
    // ════════════════════════════════════════════════════════════════════
    [HttpPost("join")]
    public async Task<IActionResult> JoinCourse([FromBody] JoinCourseDto dto)
    {
        try
        {
            if (dto.UserId == Guid.Empty)
                return BadRequest(new { message = "UserId zorunludur." });

            var courses = await _courseRepository
                .FindAsync(c => c.CourseCode == dto.CourseCode.ToUpper());
            var course = courses.FirstOrDefault();

            if (course == null)
                return NotFound(new { message = "Bu koda sahip bir ders bulunamadı." });

            // Zaten üye mi?
            var alreadyJoined = await _membershipRepository
                .FindAsync(m => m.CourseId == course.Id && m.UserId == dto.UserId);
            if (alreadyJoined.Any())
                return Conflict(new { message = "Bu derse zaten kayıtlısınız." });

            var membership = new CourseMembership
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                UserId = dto.UserId,
                IsRepresentative = false,
                JoinDate = DateTime.UtcNow
            };

            await _membershipRepository.AddAsync(membership);

            return Ok(new { message = $"'{course.Name}' dersine başarıyla katıldınız!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Derse katılırken hata oluştu: {ex.Message}" });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // GET api/courses/my/{userId}
    // Kullanıcının kayıtlı olduğu tüm dersleri getir
    // ════════════════════════════════════════════════════════════════════
    [HttpGet("my/{userId:guid}")]
    public async Task<IActionResult> GetMyCourses(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
                return BadRequest(new { message = "Geçerli bir userId giriniz." });

            var courses = await _context.CourseMemberships
                .Where(cm => cm.UserId == userId)
                .Include(cm => cm.Course)
                    .ThenInclude(c => c.Academic)
                .Include(cm => cm.Course)
                    .ThenInclude(c => c.CourseMemberships)
                .Select(cm => new
                {
                    id = cm.Course.Id,
                    name = cm.Course.Name,
                    courseCode = cm.Course.CourseCode,
                    academicName = cm.Course.Academic.FullName,
                    memberCount = cm.Course.CourseMemberships.Count,
                    isRepresentative = cm.IsRepresentative,
                    isOfficial = cm.Course.IsOfficial
                })
                .ToListAsync();

            return Ok(new { courses });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Dersler alınamadı: {ex.Message}" });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // POST api/courses/{courseId}/assign-representative
    // Temsilci ata / geri al (sadece dersin sahibi akademisyen yapabilir)
    // ════════════════════════════════════════════════════════════════════
    [HttpPost("{courseId:guid}/assign-representative")]
    public async Task<IActionResult> AssignRepresentative(
        Guid courseId,
        [FromBody] AssignRepresentativeDto dto)
    {
        try
        {
            if (dto.AcademicId == Guid.Empty || dto.StudentId == Guid.Empty)
                return BadRequest(new { message = "AcademicId ve StudentId zorunludur." });

            // 1. Dersi bul
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound(new { message = "Ders bulunamadı." });

            // 2. Çağıranın bu dersin sahibi akademisyen olduğunu doğrula
            if (course.AcademicId != dto.AcademicId)
                return Forbid(); // 403 — başka birinin dersini yönetemezsin

            // 3. Akademisyen gerçekten Akademisyen mi?
            var academic = await _userRepository.GetByIdAsync(dto.AcademicId);
            if (academic == null || academic.Role is not (UserRole.Academic or UserRole.Admin))
                return Forbid();

            // 4. Hedef öğrencinin üyeliğini bul
            var membership = await _context.CourseMemberships
                .FirstOrDefaultAsync(m => m.CourseId == courseId && m.UserId == dto.StudentId);

            if (membership == null)
                return NotFound(new { message = "Belirtilen öğrenci bu derse kayıtlı değil." });

            // 5. Hedef kullanıcının rolünü kontrol et (yalnızca öğrenci veya mevcut temsilci)
            var targetUser = await _userRepository.GetByIdAsync(dto.StudentId);
            if (targetUser == null)
                return NotFound(new { message = "Öğrenci bulunamadı." });

            if (targetUser.Role is not (UserRole.Student or UserRole.Representative))
                return BadRequest(new { message = "Yalnızca öğrenciler temsilci yapılabilir." });

            // 6. İşlemi uygula
            membership.IsRepresentative = !dto.Revoke;
            await _membershipRepository.UpdateAsync(membership);

            string actionText = dto.Revoke ? "temsilcilikten alındı" : "temsilci yapıldı";
            return Ok(new
            {
                message = $"{targetUser.FullName} başarıyla {actionText}.",
                isRepresentative = membership.IsRepresentative,
                studentId = dto.StudentId,
                courseId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"İşlem sırasında hata: {ex.Message}" });
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // GET api/courses/{courseId}/members
    // Bir dersin üye listesini getir (temsilci atama ekranı için)
    // ════════════════════════════════════════════════════════════════════
    [HttpGet("{courseId:guid}/members")]
    public async Task<IActionResult> GetCourseMembers(Guid courseId)
    {
        try
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound(new { message = "Ders bulunamadı." });

            var members = await _context.CourseMemberships
                .Where(cm => cm.CourseId == courseId)
                .Include(cm => cm.User)
                .Select(cm => new
                {
                    userId = cm.UserId,
                    fullName = cm.User.FullName,
                    email = cm.User.Email,
                    role = (int)cm.User.Role,
                    isRepresentative = cm.IsRepresentative
                })
                .ToListAsync();

            return Ok(new { members });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Üyeler alınamadı: {ex.Message}" });
        }
    }

    // ── İç DTO ───────────────────────────────────────────────────────────
    public class JoinCourseDto
    {
        public string CourseCode { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }
}
