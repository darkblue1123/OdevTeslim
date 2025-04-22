using OdevTeslim.Models; // Course modeli için gerekli
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OdevTeslim.Repositories
{
    public interface ICourseRepository : IGenericRepository<Course>
    {
        Task<Course?> GetCourseWithTeacherAsync(int courseId);
        Task<Course?> GetCourseWithAssignmentsAsync(int courseId);
        Task<IEnumerable<Course>> GetCoursesByTeacherAsync(string teacherId);
        Task<Course?> GetCourseWithEnrollmentsAndStudentsAsync(int courseId);
        // İhtiyaç duyacağın diğer özel metotlar...
    }
}