namespace OdevTeslim.DTOs
{
    public class EnrollmentDto : BaseDto
    {
        public DateTime EnrollmentDate { get; set; }
        public int CourseId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        // İsteğe bağlı olarak eşleme sırasında doldurulacak ek bilgiler:
        public string? CourseName { get; set; }
        public string? StudentFirstName { get; set; }
        public string? StudentLastName { get; set; }

    }
}
