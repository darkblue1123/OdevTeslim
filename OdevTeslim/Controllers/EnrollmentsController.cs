using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Hata yönetimi için
using OdevTeslim.DTOs; // DTO'lar
using OdevTeslim.Models; // Modeller ve DbContext
using OdevTeslim.Repositories; // Repository Interface'leri
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OdevTeslim.Controllers
{
    // Bu Controller'daki tüm işlemler belirli bir kurs bağlamında yapılır.
    // Route: /api/courses/{courseId}/enrollments
    [Route("api/courses/{courseId:int}/enrollments")] // courseId'nin integer olduğunu belirtiyoruz
    [ApiController]
    [Authorize(Roles = "Admin,Teacher")] // Genel olarak Admin veya Öğretmen erişebilir
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context; // SaveChangesAsync için

        public EnrollmentsController(
            IEnrollmentRepository enrollmentRepository,
            ICourseRepository courseRepository,
            UserManager<AppUser> userManager,
            AppDbContext context)
        {
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _userManager = userManager;
            _context = context;
        }

        // GET: api/courses/{courseId}/enrollments
        /// <summary>
        /// Belirtilen derse kayıtlı tüm öğrencileri listeler. (Admin veya Dersin Öğretmeni erişebilir)
        /// </summary>
        /// <param name="courseId">Ders ID'si</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetCourseEnrollments(int courseId)
        {
            // 1. Kurs var mı ve yetki kontrolü (Admin veya dersin öğretmeni mi?)
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Kurs bulunamadı." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && course.TeacherId != currentUserId)
            {
                return Forbid(); // Yetkisi yok (403)
            }

            // 2. Kayıtları al (Repository metodunun ilişkili Student verisini getirdiğini varsayalım)
            var enrollments = await _enrollmentRepository.GetEnrollmentsByCourseIdAsync(courseId);

            // 3. DTO'ya map etme (Manuel - AutoMapper ile daha kolay)
            var enrollmentDtos = new List<EnrollmentDto>();
            foreach (var enrollment in enrollments)
            {
                // Repository Include etmediyse öğrenciyi burada bulmamız gerekir (N+1 riski!)
                // var student = enrollment.Student ?? await _userManager.FindByIdAsync(enrollment.StudentId);
                // İdeal olanı Repository'nin öğrenciyi Include etmesidir.
                // EnrollmentRepository.GetEnrollmentsByCourseIdAsync metodunu buna göre güncelleyin.

                enrollmentDtos.Add(new EnrollmentDto
                {
                    Id = enrollment.Id,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    CourseId = enrollment.CourseId,
                    StudentId = enrollment.StudentId,
                    CourseName = course.Name, // Kurs adı zaten var
                    StudentFirstName = enrollment.Student?.FirstName, // Student Include edildiyse
                    StudentLastName = enrollment.Student?.LastName, // Student Include edildiyse
                });
            }

            return Ok(enrollmentDtos);
        }

        // POST: api/courses/{courseId}/enrollments
        /// <summary>
        /// Belirtilen derse bir öğrenci kaydeder. (Admin veya Dersin Öğretmeni erişebilir)
        /// </summary>
        /// <param name="courseId">Ders ID'si</param>
        /// <param name="enrollmentCreateDto">Kaydedilecek öğrencinin ID'sini içeren DTO</param>
        [HttpPost]
        public async Task<ActionResult<EnrollmentDto>> EnrollStudent(int courseId, [FromBody] EnrollmentCreateDto enrollmentCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Kurs var mı ve yetki kontrolü
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Kurs bulunamadı." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && course.TeacherId != currentUserId)
            {
                return Forbid(); // Yetkisi yok (403)
            }

            // 2. Öğrenci var mı ve rolü "Student" mı kontrol et
            var student = await _userManager.FindByIdAsync(enrollmentCreateDto.StudentId);
            if (student == null)
            {
                return BadRequest(new ResultDto { Status = false, Message = $"'{enrollmentCreateDto.StudentId}' ID'li öğrenci bulunamadı." });
            }
            if (!await _userManager.IsInRoleAsync(student, "Student"))
            {
                return BadRequest(new ResultDto { Status = false, Message = "Belirtilen kullanıcı bir öğrenci değil." });
            }

            // 3. Öğrenci zaten bu derse kayıtlı mı kontrol et
            var existingEnrollment = await _enrollmentRepository.GetEnrollmentByStudentAndCourseAsync(enrollmentCreateDto.StudentId, courseId);
            if (existingEnrollment != null)
            {
                return Conflict(new ResultDto { Status = false, Message = "Bu öğrenci zaten bu derse kayıtlı." }); // 409 Conflict
            }

            // 4. Kayıt oluştur
            var newEnrollment = new CourseEnrollment
            {
                CourseId = courseId,
                StudentId = enrollmentCreateDto.StudentId,
                EnrollmentDate = DateTime.UtcNow // BaseEntity'de CreatedDate olabilir, kontrol edin
            };

            await _enrollmentRepository.AddAsync(newEnrollment);

            try
            {
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Kayıt oluşturulurken bir veritabanı hatası oluştu." + ex.Message });
            }

            // 5. Başarılı yanıtı oluşturulan DTO ile döndür
            var enrollmentDto = new EnrollmentDto
            {
                Id = newEnrollment.Id,
                EnrollmentDate = newEnrollment.EnrollmentDate,
                CourseId = newEnrollment.CourseId,
                StudentId = newEnrollment.StudentId,
                CourseName = course.Name,
                StudentFirstName = student.FirstName,
                StudentLastName = student.LastName,
                
            };

            // Oluşturulan kaynağın adresi ve kendisi ile 201 Created döndür
            // Yeni kaynağın Get metodu olmadığı için şimdilik sadece Ok dönelim veya sabit bir adres verelim.
            // İdealde GET /api/enrollments/{id} gibi bir endpoint olurdu.
            // return CreatedAtAction(nameof(GetEnrollmentById), new { id = newEnrollment.Id }, enrollmentDto); // GetEnrollmentById action'ı yok
            return StatusCode(StatusCodes.Status201Created, enrollmentDto); // 201 Created
        }


        // DELETE: api/courses/{courseId}/enrollments/{studentId}
        /// <summary>
        /// Belirtilen dersten bir öğrencinin kaydını siler. (Admin veya Dersin Öğretmeni erişebilir)
        /// </summary>
        /// <param name="courseId">Ders ID'si</param>
        /// <param name="studentId">Öğrenci ID'si</param>
        [HttpDelete("{studentId}")] // Route parametresi olarak studentId
        public async Task<IActionResult> RemoveEnrollment(int courseId, string studentId)
        {
            // 1. Kurs var mı ve yetki kontrolü
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Kurs bulunamadı." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && course.TeacherId != currentUserId)
            {
                return Forbid(); // Yetkisi yok (403)
            }

            // 2. Silinecek kaydı bul
            var enrollmentToDelete = await _enrollmentRepository.GetEnrollmentByStudentAndCourseAsync(studentId, courseId);
            if (enrollmentToDelete == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Belirtilen öğrencinin bu derse kaydı bulunamadı." });
            }

            // 3. Kaydı sil
            _enrollmentRepository.Delete(enrollmentToDelete);

            try
            {
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Kayıt silinirken bir veritabanı hatası oluştu." + ex.Message });
            }

            // Başarılı silme sonrası mesaj döndür veya 204 No Content
            // return Ok(new ResultDto { Status = true, Message = "Öğrenci kaydı başarıyla silindi." });
            return NoContent(); // 204 No Content
        }
    }
}
