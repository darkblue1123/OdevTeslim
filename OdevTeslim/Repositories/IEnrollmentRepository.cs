using OdevTeslim.Models; // CourseEnrollment modeli için gerekli
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    // CourseEnrollment'ın BaseEntity'den türediğini varsayıyoruz
    public interface IEnrollmentRepository : IGenericRepository<CourseEnrollment>
    {
        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByStudentIdAsync(string studentId);
        Task<IEnumerable<CourseEnrollment>> GetEnrollmentsByCourseIdAsync(int courseId);
        Task<CourseEnrollment?> GetEnrollmentByStudentAndCourseAsync(string studentId, int courseId);
        // İhtiyaç duyacağın diğer özel metotlar...
    }
}