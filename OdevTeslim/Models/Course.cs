using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.Models
{
    public class Course:BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string TeacherId { get; set; } = string.Empty; // ApplicationUser.Id string olduğu için bu da string

        // Navigation Properties
        [ForeignKey("TeacherId")]
        public virtual AppUser? Teacher { get; set; }

        public virtual ICollection<Assignment>? Assignments { get; set; }
        public virtual ICollection<CourseEnrollment>? Enrollments { get; set; }

    }
}
