using OdevTeslim.Models; // Submission modeli için gerekli
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    public interface ISubmissionRepository : IGenericRepository<Submission>
    {
        Task<IEnumerable<Submission>> GetSubmissionsByAssignmentIdAsync(int assignmentId);
        Task<IEnumerable<Submission>> GetSubmissionsByStudentIdAsync(string studentId);
        Task<Submission?> GetSubmissionWithStudentAndAssignmentAsync(int submissionId);
        // İhtiyaç duyacağın diğer özel metotlar...
    }
}