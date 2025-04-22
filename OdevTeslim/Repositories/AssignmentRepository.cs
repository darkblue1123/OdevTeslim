using Microsoft.EntityFrameworkCore;
using OdevTeslim.Models; // AppDbContext ve Assignment için gerekli
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    public class AssignmentRepository : GenericRepository<Assignment>, IAssignmentRepository
    {
        public AssignmentRepository(AppDbContext context) : base(context) { }

        public async Task<Assignment?> GetAssignmentWithCourseAsync(int assignmentId)
        {
            return await _dbSet
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
        }

        public async Task<Assignment?> GetAssignmentWithSubmissionsAsync(int assignmentId)
        {
            return await _dbSet
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByCourseAsync(int courseId)
        {
            return await _dbSet
                .Where(a => a.CourseId == courseId)
                .ToListAsync();
        }
    }
}