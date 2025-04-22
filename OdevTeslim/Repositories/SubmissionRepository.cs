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
                .Include(s => s.Student) // Gönderen öğrenci bilgisini dahil et
                .Where(s => s.AssignmentId == assignmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsByStudentIdAsync(string studentId)
        {
            return await _dbSet
                .Include(s => s.Assignment) // Ödev bilgisini dahil et
                    .ThenInclude(a => a.Course) // Ödevden kurs bilgisini dahil et
                .Where(s => s.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<Submission?> GetSubmissionWithStudentAndAssignmentAsync(int submissionId)
        {
            return await _dbSet
               .Include(s => s.Student)
               .Include(s => s.Assignment)
               .FirstOrDefaultAsync(s => s.Id == submissionId);
        }
    }
}