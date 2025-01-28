namespace TicketSaleApp.Models.Entity
{
    public class Seat
    {
        public int? SeatId { get; set; }  // Koltuk ID
        public int? FlightId { get; set; }  // Uçuş ID
        public string? SeatNumber { get; set; }  // Koltuk numarası (örneğin 1A, 1B)
        public bool IsAvailable { get; set; }  // Koltuk durumu (boş veya dolu)

        // Navigation Property
        public Flight Flight { get; set; }  // İlgili uçuş
        public ICollection<Ticket> Tickets { get; set; }  // Koltuğa sahip olan biletler
    }

}
