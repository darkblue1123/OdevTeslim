using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.Models
{
    public class Submission:BaseEntity
    {
        [Required]
        public int AssignmentId { get; set; } // Assignment.Id int olduğu için bu da int

        [Required]
        public string StudentId { get; set; } = string.Empty; // ApplicationUser.Id string olduğu için bu da string

        // SubmissionDate BaseEntity'deki CreatedDate ile aynı olabilir, isterseniz kaldırıp BaseEntity'dekini kullanabilirsiniz.
        // Ya da özellikle 'gönderim' anını tutmak için kalabilir. Karar size ait.
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        public string? Content { get; set; }

        // --- Öğretmen Tarafından Doldurulacak Alanlar ---
        public int? Grade { get; set; }
        public string? Feedback { get; set; }
        public DateTime? GradedDate { get; set; }
        public string? GradedByTeacherId { get; set; } // ApplicationUser.Id string olduğu için bu da string (nullable)


        // Navigation Properties
        [ForeignKey("AssignmentId")]
        public virtual Assignment? Assignment { get; set; }

        [ForeignKey("StudentId")]
        public virtual AppUser? Student { get; set; }

        [ForeignKey("GradedByTeacherId")]
        public virtual AppUser? GradedByTeacher { get; set; }

    }
}
