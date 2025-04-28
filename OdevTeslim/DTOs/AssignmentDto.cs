using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.DTOs
{
    public class AssignmentDto : BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime CreatedDate { get; set; } // BaseEntity'den gelen tarih
        public int CourseId { get; set; }
        public string? CourseName { get; set; }

    }

    public class AssignmentCreateDto
    {
        [Required][StringLength(250)] public string Title { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        [Required] public DateTime DueDate { get; set; }
    }

    public class AssignmentUpdateDto
    {
        [Required][StringLength(250)] public string Title { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        [Required] public DateTime DueDate { get; set; }
    }
}
