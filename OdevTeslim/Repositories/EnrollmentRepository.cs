using Microsoft.EntityFrameworkCore;
using OdevTeslim.Models; // AppDbContext ve CourseEnrollment için gerekli
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    public class EnrollmentRepository : GenericRepository<CourseEnrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByStudentIdAsync(string studentId)
        {
            return await _dbSet
                .Include(e => e.Course) // İstenirse ders bilgisi de dahil edilir
                .Where(e => e.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseIdAsync(int courseId)
        {
            return await _dbSet
                .Include(e => e.Student) // İstenirse öğrenci bilgisi de dahil edilir
                .Where(e => e.CourseId == courseId)
                .ToListAsync();
        }
        public async Task<CourseEnrollment?> GetEnrollmentByStudentAndCourseAsync(string studentId, int courseId)
        {
            return await _dbSet
               .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        }
    }
}