using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.Models
{
    public class CourseEnrollment:BaseEntity
    {
        [Required]
        public int CourseId { get; set; } // Course.Id int

        [Required]
        public string StudentId { get; set; } = string.Empty; // ApplicationUser.Id string

        // EnrollmentDate BaseEntity'deki CreatedDate ile aynı olabilir.
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("StudentId")]
        public virtual AppUser? Student { get; set; }



    }
}
