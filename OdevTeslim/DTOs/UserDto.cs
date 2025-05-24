using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.DTOs
{
    public class UserDto 
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz.")]
        [StringLength(100, MinimumLength = 3)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email boş bırakılamaz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; }

        public string? FirstName { get; set; } // AppUser modelinizdeki alanlara göre

        public string? LastName { get; set; }  // AppUser modelinizdeki alanlara göre

        public string? PhoneNumber { get; set; }

        // Not: Rolleri veya şifreyi bu DTO üzerinden güncellemeyi genellikle ayrı endpoint'lerde yaparız.
        // Şimdilik temel kullanıcı bilgilerini içeriyor.
    }

}
