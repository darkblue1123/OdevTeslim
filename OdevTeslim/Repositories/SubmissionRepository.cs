using Microsoft.EntityFrameworkCore;
using OdevTeslim.Models; // AppDbContext ve Submission için gerekli
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    public class SubmissionRepository : GenericRepository<Submission>, ISubmissionRepository
    {
        public SubmissionRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Submission>> GetSubmissionsByAssignmentIdAsync(int assignmentId)
        {
            return await _dbSet
                .Include(s => s.Student) // Öğrenci bilgisini dahil et
                .Where(s => s.AssignmentId == assignmentId)
                .OrderByDescending(s => s.SubmissionDate) // En son teslimler üste gelsin
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsByStudentIdAsync(string studentId)
        {
            return await _dbSet
               .Include(s => s.Assignment) // Ödev bilgisini dahil et
               .Where(s => s.StudentId == studentId)
               .OrderByDescending(s => s.SubmissionDate)
               .ToListAsync();
        }

        public async Task<Submission?> GetSubmissionWithDetailsAsync(int submissionId)
        {
            return await _dbSet
               .Include(s => s.Student)     // Öğrenci
               .Include(s => s.Assignment) // Ödev
                   .ThenInclude(a => a.Course) // Ödevin Kursu
                       .ThenInclude(c => c.Teacher) // Kursun Öğretmeni
               .Include(s => s.GradedByTeacher) // Notu Veren Öğretmen
               .FirstOrDefaultAsync(s => s.Id == submissionId);
        }

        public async Task<Submission?> FindByStudentAndAssignmentAsync(string studentId, int assignmentId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.AssignmentId == assignmentId);
        }
    }
}