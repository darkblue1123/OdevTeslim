using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OdevTeslim.DTOs;
using OdevTeslim.Models;
using OdevTeslim.Repositories;
using System.Security.Claims;

namespace OdevTeslim.Controllers
{
    [Route("api/assignments/{assignmentId:int}/submissions")]
    [ApiController]
    [Authorize] // Genel olarak giriş gerekli
    public class AssignmentSubmissionsController : ControllerBase
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseRepository _courseRepository; // Yetki kontrolü için
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public AssignmentSubmissionsController(
            ISubmissionRepository submissionRepository,
            IAssignmentRepository assignmentRepository,
            IEnrollmentRepository enrollmentRepository,
            ICourseRepository courseRepository,
            UserManager<AppUser> userManager,
            AppDbContext context)
        {
            _submissionRepository = submissionRepository;
            _assignmentRepository = assignmentRepository;
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _userManager = userManager;
            _context = context;
        }

        // POST: api/assignments/{assignmentId}/submissions
        /// <summary>
        /// Belirtilen ödeve teslim yapar. (Sadece derse kayıtlı Öğrenci erişebilir)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<SubmissionDto>> CreateSubmission(int assignmentId, [FromBody] SubmissionCreateDto submissionCreateDto)
        {
            if (!ModelState.IsValid) // DTO'da validation varsa
            {
                return BadRequest(ModelState);
            }

            // 1. Ödev var mı ve detaylarını al (Kurs ID'si için)
            var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
            if (assignment == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Ödev bulunamadı." });
            }

            // 2. Teslim tarihi geçmiş mi kontrol et
            if (DateTime.UtcNow > assignment.DueDate)
            {
                return BadRequest(new ResultDto { Status = false, Message = "Bu ödevin teslim tarihi geçmiştir." });
            }


            // 3. Öğrenci ID'sini al
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (studentId == null) return Unauthorized();

            // 4. Öğrenci bu ödevin dersine kayıtlı mı kontrol et
            var enrollment = await _enrollmentRepository.GetEnrollmentByStudentAndCourseAsync(studentId, assignment.CourseId);
            if (enrollment == null)
            {
                return Forbid(); // Derse kayıtlı değilse teslim yapamaz
            }

            // 5. Bu ödeve daha önce teslim yapmış mı kontrol et (Tek teslim hakkı varsa)
            var existingSubmission = await _submissionRepository.FindByStudentAndAssignmentAsync(studentId, assignmentId);
            if (existingSubmission != null)
            {
                // İsteğe bağlı: Güncellemeye izin verilebilir veya hata döndürülebilir.
                // Şimdilik hata döndürelim.
                return Conflict(new ResultDto { Status = false, Message = "Bu ödeve zaten bir teslim yaptınız." });
            }

            // 6. Yeni teslim oluştur
            var newSubmission = new Submission
            {
                AssignmentId = assignmentId,
                StudentId = studentId,
                Content = submissionCreateDto.Content, // Şimdilik sadece metin
                SubmissionDate = DateTime.UtcNow
                // Grade, Feedback vb. başlangıçta null olacak
            };

            await _submissionRepository.AddAsync(newSubmission);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Teslim kaydedilirken bir hata oluştu." + ex.Message });
            }

            // 7. Başarılı yanıtı oluşturulan DTO ile döndür
            var student = await _userManager.FindByIdAsync(studentId); // Öğrenci bilgilerini al
            var submissionDto = new SubmissionDto
            {
                Id = newSubmission.Id,
                SubmissionDate = newSubmission.SubmissionDate,
                Content = newSubmission.Content,
                AssignmentId = newSubmission.AssignmentId,
                StudentId = newSubmission.StudentId,
                StudentFirstName = student?.FirstName,
                StudentLastName = student?.LastName,
               
                // Diğer alanlar null
            };

            // Oluşturulan kaynağın adresi SubmissionsController'daki GetSubmissionById'yi göstermeli
            // Bu yüzden CreatedAtRoute veya CreatedAtAction kullanmak daha doğru.
            return CreatedAtAction(
               nameof(SubmissionsController.GetSubmissionById), // Diğer controller'daki action adı
               "Submissions", // Diğer controller'ın adı (Route'dan türetilir, kontrol edin)
               new { submissionId = newSubmission.Id }, // Route parametreleri
               submissionDto);
            // return StatusCode(StatusCodes.Status201Created, submissionDto); // Alternatif
        }


        // GET: api/assignments/{assignmentId}/submissions
        /// <summary>
        /// Belirtilen ödeve ait tüm teslimleri listeler. (Admin veya dersin Öğretmeni erişebilir)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetAssignmentSubmissions(int assignmentId)
        {
            // 1. Ödev var mı ve kursunu getir (Yetki kontrolü için)
            var assignment = await _assignmentRepository.GetAssignmentWithCourseAsync(assignmentId);
            if (assignment == null || assignment.Course == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Ödev veya ilişkili kurs bulunamadı." });
            }

            // 2. Yetki Kontrolü (Admin veya dersin öğretmeni mi?)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && assignment.Course.TeacherId != currentUserId)
            {
                return Forbid();
            }

            // 3. Teslimleri al (Repository Student bilgisini içermeli)
            var submissions = await _submissionRepository.GetSubmissionsByAssignmentIdAsync(assignmentId);

            // 4. DTO'ya map etme
            var submissionDtos = submissions.Select(s => new SubmissionDto
            {
                Id = s.Id,
                SubmissionDate = s.SubmissionDate,
                Content = s.Content, // Kısaltılmış içerik gösterilebilir?
                Grade = s.Grade,
                Feedback = s.Feedback, // Kısaltılmış feedback?
                GradedDate = s.GradedDate,
                AssignmentId = s.AssignmentId,
                StudentId = s.StudentId,
                StudentFirstName = s.Student?.FirstName,
                StudentLastName = s.Student?.LastName,
               
                // GradedByTeacherName için ek sorgu/include gerekebilir
            }).ToList();

            return Ok(submissionDtos);
        }
    }
}
