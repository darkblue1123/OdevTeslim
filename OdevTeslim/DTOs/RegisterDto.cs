using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.DTOs
{
    public class RegisterDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }

        [Required(ErrorMessage = "Kayıt türü (rol) seçimi zorunludur.")]
        public string SelectedRole { get; set; } // YENİ EKLENEN ALAN (Örn: "Student" veya "Teacher")
    }

}

