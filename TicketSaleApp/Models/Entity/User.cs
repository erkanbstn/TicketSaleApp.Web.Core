namespace TicketSaleApp.Models.Entity
{
    public class User
    {
        public int? UserId { get; set; }  // Kullanıcı ID
        public string? Email { get; set; }  // Kullanıcı email adresi
        public string? Password { get; set; }  // Parola (hashlenmiş)
        public string? FirstName { get; set; }  // Kullanıcı adı
        public string? LastName { get; set; }  // Kullanıcı soyadı
        public bool? Status { get; set; }  // Kullanıcı durumu (aktif/pasif)
        public DateTime? CreatedAt { get; set; }  // Hesap oluşturulma tarihi

        // Navigation Property'ler
        public ICollection<Ticket> Tickets { get; set; }  // Kullanıcının sahip olduğu biletler
    }
}