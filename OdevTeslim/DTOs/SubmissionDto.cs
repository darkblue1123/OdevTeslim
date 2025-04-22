namespace OdevTeslim.DTOs
{
    public class SubmissionDto:BaseDto
    {
        public DateTime SubmissionDate { get; set; }
        public string? Content { get; set; }
        public int? Grade { get; set; }
        public string? Feedback { get; set; }
        public DateTime? GradedDate { get; set; }
        public int AssignmentId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string? StudentFirstName { get; set; }
        public string? StudentLastName { get; set; }
        public string? GradedByTeacherName { get; set; }

    }
}
