using System.ComponentModel.DataAnnotations;

namespace OdevTeslim.Models
{
    public class BaseEntity
    {
        [Key] // Primary Key olduğunu belirtir
        public int Id { get; set; } // Tüm varlıklar için ortak ID alanı

        // Opsiyonel: Ortak takip alanları
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; } // Nullable, çünkü ilk başta null olabilir
    }
}
