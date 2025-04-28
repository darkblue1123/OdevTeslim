using OdevTeslim.Models; // Submission modeli için gerekli
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    public interface ISubmissionRepository : IGenericRepository<Submission>
    {
        Task<IEnumerable<Submission>> GetSubmissionsByAssignmentIdAsync(int assignmentId);
        Task<IEnumerable<Submission>> GetSubmissionsByStudentIdAsync(string studentId);
        Task<Submission?> GetSubmissionWithDetailsAsync(int submissionId); // Student, Assignment içerir
        Task<Submission?> FindByStudentAndAssignmentAsync(string studentId, int assignmentId); // Öğrencinin ödeve teslim yapıp yapmadığını kontrol etmek için
    }
}