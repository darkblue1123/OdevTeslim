using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Hata yönetimi için
using OdevTeslim.DTOs;
using OdevTeslim.Models;
using OdevTeslim.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OdevTeslim.Controllers
{
    // Tüm işlemler belirli bir kurs bağlamında: /api/courses/{courseId}/assignments
    [Route("api/courses/{courseId:int}/assignments")]
    [ApiController]
    [Authorize] // Genel olarak giriş yapmış olmak gerekir
    public class AssignmentsController : ControllerBase
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ICourseRepository _courseRepository; // Kurs kontrolü ve yetkilendirme için
        private readonly UserManager<AppUser> _userManager; // Yetkilendirme için
        private readonly AppDbContext _context; // SaveChangesAsync için
        private readonly IEnrollmentRepository _enrollmentRepository; // Öğrenci yetkilendirme kontrolü için


        public AssignmentsController(
            IAssignmentRepository assignmentRepository,
            ICourseRepository courseRepository,
            UserManager<AppUser> userManager,
            AppDbContext context,
            IEnrollmentRepository enrollmentRepository)
        {
            _assignmentRepository = assignmentRepository;
            _courseRepository = courseRepository;
            _userManager = userManager;
            _context = context;
            _enrollmentRepository = enrollmentRepository;
        }

        // GET: api/courses/{courseId}/assignments
        /// <summary>
        /// Belirtilen kursa ait tüm ödevleri listeler.
        /// (Admin, dersin Öğretmeni veya derse kayıtlı Öğrenci erişebilir)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetCourseAssignments(int courseId)
        {
            // 1. Kurs var mı kontrol et
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "İlgili kurs bulunamadı." });
            }

            // 2. Yetki Kontrolü (Admin veya Öğretmen ise sorun yok, Öğrenci ise derse kayıtlı mı?)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isTeacherOrAdmin = User.IsInRole("Admin") || (User.IsInRole("Teacher") && course.TeacherId == currentUserId);

            if (!isTeacherOrAdmin && User.IsInRole("Student"))
            {
                var enrollment = await _enrollmentRepository.GetEnrollmentByStudentAndCourseAsync(currentUserId!, courseId);
                if (enrollment == null)
                {
                    // Öğrenci bu derse kayıtlı değilse yetkisi yok
                    return Forbid(); // 403 Forbidden
                }
            }
            else if (!isTeacherOrAdmin) // Öğrenci rolünde de değilse veya başka bir durumsa
            {
                return Forbid();
            }


            // 3. Ödevleri al
            var assignments = await _assignmentRepository.GetAssignmentsByCourseAsync(courseId);

            // 4. DTO'ya map etme (Manuel)
            var assignmentDtos = assignments.Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                DueDate = a.DueDate,
                CreatedDate = a.CreatedDate,
                CourseId = a.CourseId,
                CourseName = course.Name // Kurs adını ekleyelim
            }).ToList();

            return Ok(assignmentDtos);
        }

        // GET: api/courses/{courseId}/assignments/{assignmentId}
        /// <summary>
        /// Belirtilen kurstaki belirli bir ödevin detayını getirir.
        /// (Admin, dersin Öğretmeni veya derse kayıtlı Öğrenci erişebilir)
        /// </summary>
        [HttpGet("{assignmentId:int}")] // Route: api/courses/{courseId}/assignments/{assignmentId}
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<AssignmentDto>> GetAssignment(int courseId, int assignmentId)
        {
            // 1. Ödevi ve ilişkili kursu getir
            var assignment = await _assignmentRepository.GetAssignmentWithCourseAsync(assignmentId);

            if (assignment == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Ödev bulunamadı." });
            }

            // 2. URL'deki courseId ile ödevin ait olduğu courseId eşleşiyor mu kontrol et
            if (assignment.CourseId != courseId)
            {
                return BadRequest(new ResultDto { Status = false, Message = "URL'deki kurs ID'si ile ödevin ait olduğu kurs ID'si eşleşmiyor." });
            }

            // 3. Yetki Kontrolü (Admin, dersin Öğretmeni veya derse kayıtlı Öğrenci)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isTeacherOrAdmin = User.IsInRole("Admin") || (User.IsInRole("Teacher") && assignment.Course?.TeacherId == currentUserId);

            if (!isTeacherOrAdmin && User.IsInRole("Student"))
            {
                var enrollment = await _enrollmentRepository.GetEnrollmentByStudentAndCourseAsync(currentUserId!, courseId);
                if (enrollment == null) return Forbid();
            }
            else if (!isTeacherOrAdmin)
            {
                return Forbid();
            }

            // 4. DTO'ya map etme
            var assignmentDto = new AssignmentDto
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                CreatedDate = assignment.CreatedDate,
                CourseId = assignment.CourseId,
                CourseName = assignment.Course?.Name // İlişkili kursun adı
            };

            return Ok(assignmentDto);
        }


        // POST: api/courses/{courseId}/assignments
        /// <summary>
        /// Belirtilen kursa yeni bir ödev ekler. (Admin veya dersin Öğretmeni erişebilir)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<AssignmentDto>> CreateAssignment(int courseId, [FromBody] AssignmentCreateDto assignmentCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Kurs var mı ve yetki kontrolü (Admin veya dersin öğretmeni mi?)
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Ödev eklenecek kurs bulunamadı." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && course.TeacherId != currentUserId)
            {
                return Forbid(); // Yetkisi yok
            }

            // 2. Yeni ödev entity'sini oluştur
            var newAssignment = new Assignment
            {
                Title = assignmentCreateDto.Title,
                Description = assignmentCreateDto.Description,
                DueDate = assignmentCreateDto.DueDate,
                CourseId = courseId, // URL'den gelen courseId atanıyor
                CreatedDate = DateTime.UtcNow // BaseEntity'de otomatik olabilir
            };

            await _assignmentRepository.AddAsync(newAssignment);
            try
            {
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Ödev kaydedilirken bir hata oluştu." + ex.Message });
            }

            // 3. Oluşturulan kaynağı DTO olarak döndür
            var assignmentDto = new AssignmentDto
            {
                Id = newAssignment.Id,
                Title = newAssignment.Title,
                Description = newAssignment.Description,
                DueDate = newAssignment.DueDate,
                CreatedDate = newAssignment.CreatedDate,
                CourseId = newAssignment.CourseId,
                CourseName = course.Name
            };

            // Oluşturulan kaynağın adresini ve kendisini döndür (GetAssignment action'ına referans verir)
            return CreatedAtAction(nameof(GetAssignment), new { courseId = courseId, assignmentId = newAssignment.Id }, assignmentDto);
        }


        // PUT: api/courses/{courseId}/assignments/{assignmentId}
        /// <summary>
        /// Belirtilen kurstaki belirli bir ödevi günceller. (Admin veya dersin Öğretmeni erişebilir)
        /// </summary>
        [HttpPut("{assignmentId:int}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> UpdateAssignment(int courseId, int assignmentId, [FromBody] AssignmentUpdateDto assignmentUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Güncellenecek ödevi bul
            var assignmentToUpdate = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignmentToUpdate == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Güncellenecek ödev bulunamadı." });
            }

            // 2. URL'deki courseId ile ödevin ait olduğu courseId eşleşiyor mu kontrol et
            if (assignmentToUpdate.CourseId != courseId)
            {
                return BadRequest(new ResultDto { Status = false, Message = "URL'deki kurs ID'si ile ödevin ait olduğu kurs ID'si eşleşmiyor." });
            }

            // 3. Yetki kontrolü (Admin veya dersin öğretmeni mi?)
            var course = await _courseRepository.GetByIdAsync(courseId); // Kurs bilgisini al (TeacherId için)
            if (course == null) return BadRequest(new ResultDto { Status = false, Message = "İlişkili kurs bulunamadı." }); // Ekstra kontrol

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && course.TeacherId != currentUserId)
            {
                return Forbid(); // Yetkisi yok
            }

            // 4. Entity'yi güncelle
            assignmentToUpdate.Title = assignmentUpdateDto.Title;
            assignmentToUpdate.Description = assignmentUpdateDto.Description;
            assignmentToUpdate.DueDate = assignmentUpdateDto.DueDate;
            // ModifiedDate BaseEntity'de otomatik olabilir veya burada set edilir

            // _assignmentRepository.Update(assignmentToUpdate); // EF Core zaten takip ediyor olabilir

            try
            {
                // Emin olmak için state'i Modified yapmak veya Update çağırmak iyi olabilir
                _context.Assignments.Update(assignmentToUpdate); // Veya _context.Entry(assignmentToUpdate).State = EntityState.Modified;
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new ResultDto { Status = false, Message = "Ödev güncellenirken çakışma yaşandı, lütfen tekrar deneyin." });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Ödev güncellenirken bir hata oluştu." + ex.Message });
            }


            return NoContent(); // Başarılı güncelleme sonrası 204
        }


        // DELETE: api/courses/{courseId}/assignments/{assignmentId}
        /// <summary>
        /// Belirtilen kurstaki belirli bir ödevi siler. (Admin veya dersin Öğretmeni erişebilir)
        /// </summary>
        [HttpDelete("{assignmentId:int}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteAssignment(int courseId, int assignmentId)
        {
            // 1. Silinecek ödevi bul
            var assignmentToDelete = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignmentToDelete == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Silinecek ödev bulunamadı." });
            }

            // 2. URL'deki courseId ile ödevin ait olduğu courseId eşleşiyor mu kontrol et
            if (assignmentToDelete.CourseId != courseId)
            {
                return BadRequest(new ResultDto { Status = false, Message = "URL'deki kurs ID'si ile ödevin ait olduğu kurs ID'si eşleşmiyor." });
            }

            // 3. Yetki kontrolü (Admin veya dersin öğretmeni mi?)
            var course = await _courseRepository.GetByIdAsync(courseId); // Kurs bilgisini al (TeacherId için)
            if (course == null) return BadRequest(new ResultDto { Status = false, Message = "İlişkili kurs bulunamadı." }); // Ekstra kontrol

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && course.TeacherId != currentUserId)
            {
                return Forbid(); // Yetkisi yok
            }

            // 4. Ödevi sil
            _assignmentRepository.Delete(assignmentToDelete);

            try
            {
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateException ex) // İlişkili Submission'lar varsa ve silme kısıtlıysa
            {
                // Loglama yapılabilir
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Ödev silinirken bir veritabanı hatası oluştu. İlişkili teslimler olabilir." + ex.Message });
            }

            return NoContent(); // Başarılı silme sonrası 204
        }
    }
}