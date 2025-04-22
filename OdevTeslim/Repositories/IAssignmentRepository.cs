using OdevTeslim.Models; // Assignment modeli için gerekli
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    public interface IAssignmentRepository : IGenericRepository<Assignment>
    {
        Task<Assignment?> GetAssignmentWithCourseAsync(int assignmentId);
        Task<Assignment?> GetAssignmentWithSubmissionsAsync(int assignmentId);
        Task<IEnumerable<Assignment>> GetAssignmentsByCourseAsync(int courseId);
        // İhtiyaç duyacağın diğer özel metotlar...
    }
}