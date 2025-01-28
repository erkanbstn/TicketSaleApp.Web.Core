namespace TicketSaleApp.Models.Entity
{
    public enum TicketStatus
    {
        Beklemede = 0,
        Satıldı = 1,
        İptalEdildi = 2
    }

    public class Ticket
    {
        public int? TicketId { get; set; }  // Bilet ID
        public int? UserId { get; set; }  // Kullanıcı ID
        public int? FlightId { get; set; }  // Uçuş ID
        public int? SeatId { get; set; }  // Koltuk ID
        public DateTime PurchaseDate { get; set; }  // Bilet satın alma tarihi
        public TicketStatus? Status { get; set; }  // Bilet durumu

        // Navigation Property'ler
        public User User { get; set; }  // Bilet sahibinin kullanıcısı
        public Flight Flight { get; set; }  // Uçuş bilgisi
        public Seat Seat { get; set; }  // Koltuk bilgisi
    }
}
