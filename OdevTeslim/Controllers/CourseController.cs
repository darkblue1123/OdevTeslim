using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity; // UserManager için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Include için (opsiyonel, repository'de yapılabilir)
using OdevTeslim.DTOs; // DTO'lar için
using OdevTeslim.Models; // AppUser, Course ve AppDbContext için
using OdevTeslim.Repositories; // ICourseRepository için
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims; // Kullanıcı ID'sini almak için
using System.Threading.Tasks;

namespace OdevTeslim.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CoursesController : ControllerBase 
    {
        private readonly ICourseRepository _courseRepository;
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CoursesController(
            ICourseRepository courseRepository,
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _courseRepository = courseRepository;
            _context = context;
            _userManager = userManager;
        }

        
        /// <summary>
        /// Tüm dersleri listeler. (Admin, Öğretmen, Öğrenci erişebilir)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses()
        {
            var courses = await _courseRepository.GetAllAsync();

            var courseDtos = new List<CourseDto>();
            foreach (var course in courses)
            {
                var teacher = await _context.Users.FindAsync(course.TeacherId); 

                courseDtos.Add(new CourseDto
                {
                    Id = course.Id,
                    Name = course.Name,
                    Description = course.Description,
                    TeacherId = course.TeacherId,
                    TeacherName = teacher?.UserName
                });
            }
            return Ok(courseDtos);
        }

        // GET: api/courses/{id}
        /// <summary>
        /// Belirli bir dersin detaylarını getirir. (Admin, Öğretmen, Öğrenci erişebilir)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<CourseDto>> GetCourse(int id)
        {
            var course = await _courseRepository.GetCourseWithTeacherAsync(id);

            if (course == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Kurs bulunamadı." });
            }

            var courseDto = new CourseDto
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher != null ? $"{course.Teacher.FirstName} {course.Teacher.LastName}" : null
            };

            return Ok(courseDto);
        }

        // POST: api/courses
        /// <summary>
        /// Yeni bir ders oluşturur. (Admin veya Öğretmen erişebilir)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CourseCreateDto courseCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Giriş yapan kullanıcının ID'sini al
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
               
                return Unauthorized(new ResultDto { Status = false, Message = "Kullanıcı kimliği bulunamadı." });
            }

          
            var loggedInUser = await _userManager.FindByIdAsync(userId);
            if (loggedInUser == null)
            {
                return Unauthorized(new ResultDto { Status = false, Message = "Kullanıcı bulunamadı." });
            }
           

            // Manuel Mapping (DTO -> Entity)
            var newCourse = new Course
            {
                Name = courseCreateDto.Name,
                Description = courseCreateDto.Description,
                TeacherId = userId, 
                CreatedDate = DateTime.UtcNow 
            };

            await _courseRepository.AddAsync(newCourse);
            try
            {
                await _context.SaveChangesAsync(); 
            }
            catch (DbUpdateException ex)
            {
               
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Ders kaydedilirken bir hata oluştu." + ex.Message });
            }

            // Oluşturulan kaynağı döndür (CourseDto olarak)
            var courseDto = new CourseDto
            {
                Id = newCourse.Id, // ID artık mevcut
                Name = newCourse.Name,
                Description = newCourse.Description,
                TeacherId = newCourse.TeacherId,
                TeacherName = $"{loggedInUser.FirstName} {loggedInUser.LastName}"
            };

            // 201 Created durum kodu ile birlikte oluşturulan kaynağın adresini ve kendisini döndür
            return CreatedAtAction(nameof(GetCourse), new { id = newCourse.Id }, courseDto);
        }

        // PUT: api/courses/{id}
        /// <summary>
        /// Mevcut bir dersi günceller. (Admin veya dersin sahibi Öğretmen erişebilir)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseUpdateDto courseUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var courseToUpdate = await _courseRepository.GetByIdAsync(id); 

            if (courseToUpdate == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Güncellenecek kurs bulunamadı." });
            }

          
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");

            if (!isAdmin && courseToUpdate.TeacherId != currentUserId)
            {
               
                return Forbid(); // 403 Forbidden
            }

            
            courseToUpdate.Name = courseUpdateDto.Name;
            courseToUpdate.Description = courseUpdateDto.Description;
            

            try
            {
                // Sadece state'i değiştirmek yerine Update metodunu çağırmak daha güvenli olabilir
                _context.Courses.Update(courseToUpdate); // Veya _context.Entry(courseToUpdate).State = EntityState.Modified;
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateConcurrencyException) // Aynı anda başka birisi kaydı güncellemişse
            {
                return Conflict(new ResultDto { Status = false, Message = "Kurs güncellenirken çakışma yaşandı, lütfen tekrar deneyin." });
            }
            catch (DbUpdateException ex)
            {
                // Veritabanı kaydetme hatası (loglama yapılabilir)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Kurs güncellenirken bir hata oluştu." + ex.Message });
            }

            // Başarılı güncelleme sonrası genelde 204 No Content döndürülür
            return NoContent();
        }

        // DELETE: api/courses/{id}
        /// <summary>
        /// Bir dersi siler. (Sadece Admin erişebilir - örnek senaryo)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var courseToDelete = await _courseRepository.GetByIdAsync(id);

            if (courseToDelete == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Silinecek kurs bulunamadı." });
            }

           

            _courseRepository.Delete(courseToDelete);

            try
            {
                await _context.SaveChangesAsync(); 
            }
            catch (DbUpdateException ex)
            {
                
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Kurs silinirken bir veritabanı hatası oluştu. İlişkili kayıtlar olabilir." + ex.Message });
            }


            // Başarılı silme sonrası mesaj döndür
            return Ok(new ResultDto { Status = true, Message = "Kurs başarıyla silindi." });

        }


        [HttpGet("{courseId}/details-and-students")] 
        [Authorize(Roles = "Admin,Teacher,Student")] 
        public async Task<IActionResult> GetCourseDetailsAndStudents(int courseId)
        {
            
            var courseEntity = await _courseRepository.GetCourseWithTeacherAsync(courseId);
            if (courseEntity == null)
            {
                return NotFound(new { success = false, message = $"Kurs (ID: {courseId}) bulunamadı." });
            }

          
            var courseData = new
            {
                id = courseEntity.Id,
                name = courseEntity.Name,
                description = courseEntity.Description,
                teacherName = courseEntity.Teacher != null ? $"{courseEntity.Teacher.FirstName} {courseEntity.Teacher.LastName}" : "Belirtilmemiş",
                teacherId = courseEntity.TeacherId 
            };


            
            var studentEntities = await _context.Enrollments
                .Where(ce => ce.CourseId == courseId)
                .Include(ce => ce.Student) 
                .Select(ce => ce.Student)
                .ToListAsync();

            
            var studentsData = studentEntities.Select(s => new
            {
                id = s.Id,
                fullName = $"{s.FirstName} {s.LastName}", 
                email = s.Email
                
            }).ToList();

            
            var result = new
            {
                success = true,
                courseInfo = courseData,
                enrolledStudents = studentsData
            };

            return Ok(result);
        }

        [HttpGet("{courseId}/assignments-with-details")]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> GetCourseAssignmentsWithDetails(int courseId)
        {
            
            var courseEntity = await _context.Courses 
                .AsNoTracking() 
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (courseEntity == null)
            {
                return NotFound(new { success = false, message = $"Kurs (ID: {courseId}) bulunamadı." });
            }

            
            var courseData = new
            {
                id = courseEntity.Id,
                name = courseEntity.Name,
                teacherId = courseEntity.TeacherId 
            };

           
            var mappedAssignments = await _context.Assignments
                .Where(a => a.CourseId == courseId)
                .OrderBy(a => a.DueDate)
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    description = a.Description,
                    dueDate = a.DueDate,
                    createdDate = a.CreatedDate
                })
                .ToListAsync();

            var result = new
            {
                success = true,
                courseInfo = courseData,
                
                assignments = mappedAssignments 
            };



            return Ok(result);
        }


        [HttpGet("my")] 
        [Authorize(Roles = "Uye")] 
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetMyEnrolledCourses()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ResultDto { Status = false, Message = "Kullanıcı kimliği bulunamadı." });
            }

            var enrolledCourses = await _context.Enrollments
                .Where(ce => ce.StudentId == studentId)
                .Include(ce => ce.Course) 
                    .ThenInclude(c => c.Teacher) 
                .Select(ce => ce.Course) 
                .ToListAsync();

            if (enrolledCourses == null || !enrolledCourses.Any())
            {
                return Ok(new List<CourseDto>()); 
            }

            // Kursları CourseDto'ya map'le
            var courseDtos = enrolledCourses.Select(course => new CourseDto
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher != null ? $"{course.Teacher.FirstName} {course.Teacher.LastName}" : "Belirtilmemiş"
                
            }).ToList();

            return Ok(courseDtos);
        }

    }
}