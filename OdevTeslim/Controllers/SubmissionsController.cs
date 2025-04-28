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
    [Route("api/submissions")] // Genel route
    [ApiController]
    [Authorize] // Genel olarak giriş gerekli
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly UserManager<AppUser> _userManager; // Not veren öğretmeni belirlemek için
        private readonly AppDbContext _context; // SaveChangesAsync için

        public SubmissionsController(
            ISubmissionRepository submissionRepository,
            UserManager<AppUser> userManager,
            AppDbContext context)
        {
            _submissionRepository = submissionRepository;
            _userManager = userManager;
            _context = context;
        }

        // GET: api/submissions/{submissionId}
        /// <summary>
        /// Belirli bir teslimin detaylarını getirir.
        /// (Admin, ilgili dersin Öğretmeni veya teslimi yapan Öğrenci erişebilir)
        /// </summary>
        [HttpGet("{submissionId:int}", Name = "GetSubmissionById")] // Route ismi CreatedAtAction için
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<SubmissionDto>> GetSubmissionById(int submissionId)
        {
            // 1. Teslimi detaylarıyla (ilişkili varlıklarla) getir
            var submission = await _submissionRepository.GetSubmissionWithDetailsAsync(submissionId);
            if (submission == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Teslim bulunamadı." });
            }

            // 2. Yetki Kontrolü
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isTeacherOfCourse = User.IsInRole("Teacher") && submission.Assignment?.Course?.TeacherId == currentUserId;
            bool isAdmin = User.IsInRole("Admin");
            bool isOwnerStudent = User.IsInRole("Student") && submission.StudentId == currentUserId;

            if (!isAdmin && !isTeacherOfCourse && !isOwnerStudent)
            {
                return Forbid(); // Yetkisi yok
            }

            // 3. DTO'ya map etme
            var submissionDto = new SubmissionDto
            {
                Id = submission.Id,
                SubmissionDate = submission.SubmissionDate,
                Content = submission.Content,
                Grade = submission.Grade,
                Feedback = submission.Feedback,
                GradedDate = submission.GradedDate,
                AssignmentId = submission.AssignmentId,
                StudentId = submission.StudentId,
                StudentFirstName = submission.Student?.FirstName,
                StudentLastName = submission.Student?.LastName,
               
                GradedByTeacherName = submission.GradedByTeacher != null ? $"{submission.GradedByTeacher.FirstName} {submission.GradedByTeacher.LastName}" : null
            };

            return Ok(submissionDto);
        }


        // PUT: api/submissions/{submissionId}/grade
        /// <summary>
        /// Belirli bir teslimi notlandırır ve geri bildirim ekler. (Admin veya ilgili dersin Öğretmeni erişebilir)
        /// </summary>
        [HttpPut("{submissionId:int}/grade")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GradeSubmission(int submissionId, [FromBody] SubmissionGradeDto gradeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Teslimi detaylarıyla getir (Kurs/Öğretmen kontrolü için)
            // GetSubmissionWithDetailsAsync hem kursu hem öğretmeni getiriyor olmalı
            var submissionToGrade = await _submissionRepository.GetSubmissionWithDetailsAsync(submissionId);
            if (submissionToGrade == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Not verilecek teslim bulunamadı." });
            }

            // 2. Yetki Kontrolü (Admin veya dersin öğretmeni mi?)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isTeacherOfCourse = User.IsInRole("Teacher") && submissionToGrade.Assignment?.Course?.TeacherId == currentUserId;
            bool isAdmin = User.IsInRole("Admin");

            if (!isAdmin && !isTeacherOfCourse)
            {
                return Forbid(); // Yetkisi yok
            }

            // 3. Teslimi güncelle
            submissionToGrade.Grade = gradeDto.Grade;
            submissionToGrade.Feedback = gradeDto.Feedback;
            submissionToGrade.GradedDate = DateTime.UtcNow;
            submissionToGrade.GradedByTeacherId = currentUserId; // Notu veren öğretmenin ID'si

            // _submissionRepository.Update(submissionToGrade); // EF Core takip ediyor olabilir

            try
            {
                _context.Submissions.Update(submissionToGrade); // Veya Entry(..).State = Modified
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new ResultDto { Status = false, Message = "Teslim notlandırılırken çakışma yaşandı." });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Teslim notlandırılırken bir hata oluştu." + ex.Message });
            }

            return NoContent(); // Başarılı güncelleme sonrası 204
        }
    }
}
