using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.Models
{
    public class Assignment:BaseEntity
    {
        [Required]
        [StringLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        // CreatedDate BaseEntity'den geliyor, buradaki kaldırılabilir veya üzerine yazılabilir.
        // public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int CourseId { get; set; } // Course.Id int olduğu için bu da int

        // Navigation Properties
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        public virtual ICollection<Submission>? Submissions { get; set; }



    }
}
