using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.DTOs
{
    public class CourseDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TeacherId { get; set; } = string.Empty;
        public string? TeacherName { get; set; }

    }

    public class CourseCreateDto
    {
        [Required][StringLength(200)] public string Name { get; set; } = string.Empty;
        [StringLength(500)] public string? Description { get; set; }
    }

    public class CourseUpdateDto
    {
        [Required][StringLength(200)] public string Name { get; set; } = string.Empty;
        [StringLength(500)] public string? Description { get; set; }
    }


}
