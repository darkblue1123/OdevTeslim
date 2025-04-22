
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.Models

{
    public class AppUser: IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty; // Öğrenci veya Öğretmen Adı

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty; // Öğrenci veya Öğretmen Soyadı

        // İlişkiler (Navigation Properties)

        // Öğretmen ise yönettiği kurslar
        public virtual ICollection<Course>? TaughtCourses { get; set; }

        // Öğrenci ise kayıtlı olduğu kurslar (Enrollment üzerinden)
        public virtual ICollection<CourseEnrollment>? Enrollments { get; set; }

        // Öğrenci ise yaptığı gönderimler
        public virtual ICollection<Submission>? Submissions { get; set; }
    }
}
