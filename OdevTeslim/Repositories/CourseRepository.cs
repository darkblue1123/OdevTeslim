using Microsoft.EntityFrameworkCore;
using OdevTeslim.Models; // AppDbContext ve Course için gerekli
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(AppDbContext context) : base(context) { }

        public async Task<Course?> GetCourseWithTeacherAsync(int courseId)
        {
            return await _dbSet
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        public async Task<Course?> GetCourseWithAssignmentsAsync(int courseId)
        {
            return await _dbSet
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        public async Task<IEnumerable<Course>> GetCoursesByTeacherAsync(string teacherId)
        {
            return await _dbSet
                .Where(c => c.TeacherId == teacherId)
                .ToListAsync();
        }

        public async Task<Course?> GetCourseWithEnrollmentsAndStudentsAsync(int courseId)
        {
            return await _dbSet
               .Include(c => c.Enrollments)
                   .ThenInclude(e => e.Student) // CourseEnrollment'taki Student navigation property adı doğru mu? Kontrol et!
               .FirstOrDefaultAsync(c => c.Id == courseId);
        }
    }
}