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
using System.Security.Claims; // Kullanıcı ID'sini almak için
using System.Threading.Tasks;

namespace OdevTeslim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bu controller'a erişim için genel olarak giriş yapmış olmak gerekir
    public class CoursesController : ControllerBase
    {
        private readonly ICourseRepository _courseRepository;
        private readonly AppDbContext _context; // SaveChangesAsync için
        private readonly UserManager<AppUser> _userManager; // Öğretmen ID'sini almak/kontrol etmek için

        public CoursesController(
            ICourseRepository courseRepository,
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _courseRepository = courseRepository;
            _context = context;
            _userManager = userManager;
        }

        // GET: api/courses
        /// <summary>
        /// Tüm dersleri listeler. (Admin, Öğretmen, Öğrenci erişebilir)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses()
        {
            var courses = await _courseRepository.GetAllAsync();

            // Manuel Mapping (Entity -> DTO) - AutoMapper ile daha kolay olur
            var courseDtos = new List<CourseDto>();
            foreach (var course in courses)
            {
                // Öğretmen ismini almak için ek sorgu veya Repository'de Include gerekli
                // Şimdilik TeacherId'yi döndürelim. İsim eklemek isterseniz repository metodunu güncellemelisiniz.
                var teacher = await _context.Users.FindAsync(course.TeacherId); // Öğretmeni ID ile bul

                courseDtos.Add(new CourseDto
                {
                    Id = course.Id,
                    Name = course.Name,
                    Description = course.Description,
                    TeacherId = course.TeacherId,
                    TeacherName = teacher?.UserName // Öğretmen bulunduysa adını ata, bulunamadıysa null kalır
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
            // Öğretmen bilgisini de almak için özel repository metodunu kullanalım
            var course = await _courseRepository.GetCourseWithTeacherAsync(id);

            if (course == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Kurs bulunamadı." });
            }

            // Manuel Mapping
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
                // Bu durum normalde [Authorize] nedeniyle oluşmaz ama kontrol eklemek iyidir.
                return Unauthorized(new ResultDto { Status = false, Message = "Kullanıcı kimliği bulunamadı." });
            }

            // Giriş yapan kullanıcının Teacher rolünde olduğundan emin ol (Admin de olabilir)
            // Veya direkt TeacherId olarak bu ID'yi kullan.
            var loggedInUser = await _userManager.FindByIdAsync(userId);
            if (loggedInUser == null)
            {
                return Unauthorized(new ResultDto { Status = false, Message = "Kullanıcı bulunamadı." });
            }
            // Eğer sadece Öğretmenler ders oluşturabilsin istiyorsak ve Admin'ler değilse:
            // if (!await _userManager.IsInRoleAsync(loggedInUser, "Teacher"))
            // {
            //     return Forbid(); // Yetkisi yok (403 Forbidden)
            // }


            // Manuel Mapping (DTO -> Entity)
            var newCourse = new Course
            {
                Name = courseCreateDto.Name,
                Description = courseCreateDto.Description,
                TeacherId = userId, // Dersi oluşturan öğretmenin ID'si
                CreatedDate = DateTime.UtcNow // BaseEntity'de otomatik atanıyor olabilir, kontrol edin.
            };

            await _courseRepository.AddAsync(newCourse);
            try
            {
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateException ex)
            {
                // Veritabanı kaydetme hatası (loglama yapılabilir)
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

            var courseToUpdate = await _courseRepository.GetByIdAsync(id); // Önce varlığı al

            if (courseToUpdate == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Güncellenecek kurs bulunamadı." });
            }

            // Yetki Kontrolü: Admin değilse, sadece kendi dersini güncelleyebilir
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");

            if (!isAdmin && courseToUpdate.TeacherId != currentUserId)
            {
                // Kullanıcı Admin değil VE dersin sahibi de değilse -> Yetkisi yok
                return Forbid(); // 403 Forbidden
            }

            // Manuel Mapping (DTO -> Entity)
            courseToUpdate.Name = courseUpdateDto.Name;
            courseToUpdate.Description = courseUpdateDto.Description;
            // ModifiedDate BaseEntity'de otomatik güncelleniyor olabilir, kontrol edin.
            // Veya burada manuel olarak ayarlayın: courseToUpdate.ModifiedDate = DateTime.UtcNow;

            // EF Core zaten varlığı takip ettiği için Update metodunu çağırmak yeterli
            // _courseRepository.Update(courseToUpdate); // GenericRepository bunu yapıyor

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
        [Authorize(Roles = "Admin")] // Sadece Admin silebilir
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var courseToDelete = await _courseRepository.GetByIdAsync(id);

            if (courseToDelete == null)
            {
                return NotFound(new ResultDto { Status = false, Message = "Silinecek kurs bulunamadı." });
            }

            // Yetki kontrolü zaten [Authorize(Roles="Admin")] ile yapıldı ama ek kontrol eklenebilir.

            _courseRepository.Delete(courseToDelete);

            try
            {
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
            catch (DbUpdateException ex) // İlişkili kayıtlar varsa (ödevler, kayıtlar) ve silme kısıtlanmışsa hata verebilir
            {
                // Loglama yapılabilir
                // İlişkili kayıtları da silmek veya silme işlemini engellemek gerekebilir (veritabanı tasarımı/cascade delete ayarlarına bağlı)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResultDto { Status = false, Message = "Kurs silinirken bir veritabanı hatası oluştu. İlişkili kayıtlar olabilir." + ex.Message });
            }


            // Başarılı silme sonrası mesaj döndür
            return Ok(new ResultDto { Status = true, Message = "Kurs başarıyla silindi." });
        }
    }
}