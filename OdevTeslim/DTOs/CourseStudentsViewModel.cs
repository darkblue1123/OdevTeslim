namespace OdevTeslim.DTOs
{
    public class StudentDto
    {
        public string Id { get; set; } // Kullanıcı ID'nizin tipine göre int de olabilir
        public string FullName { get; set; }
        public string Email { get; set; }
        // Öğrenciyle ilgili göstermek istediğiniz diğer alanlar
    }


    // ~/ViewModels/CourseStudentsViewModel.cs

    public class CourseStudentsViewModel
    {
        public CourseDto CourseInfo { get; set; }
        public List<StudentDto> EnrolledStudents { get; set; }

        public CourseStudentsViewModel()
        {
            EnrolledStudents = new List<StudentDto>(); // Başlangıçta boş liste
        }

    }
}
