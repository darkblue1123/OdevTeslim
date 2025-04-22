namespace OdevTeslim.DTOs
{
    // Veri döndüren ve int Id'ye sahip DTO'lar için temel sınıf
    public abstract class BaseDto // Abstract olması, doğrudan BaseDto nesnesi oluşturulmasını engeller
    {
        public int Id { get; set; }

        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
    }
}