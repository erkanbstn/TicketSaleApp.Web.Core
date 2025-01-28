namespace TicketSaleApp.Models.Entity
{
    public class Flight
    {
        public int? FlightId { get; set; }  // Uçuş ID
        public string? FlightNumber { get; set; }  // Uçuş numarası
        public string? DepartureCity { get; set; }  // Kalkış şehri
        public string? ArrivalCity { get; set; }  // Varış şehri
        public DateTime DepartureTime { get; set; }  // Kalkış zamanı
        public DateTime ArrivalTime { get; set; }  // Varış zamanı
        public int? AvailableSeats { get; set; }  // Uçakta mevcut koltuk sayısı

        // Navigation Property'ler
        public ICollection<Ticket> Tickets { get; set; }  // Uçuştaki biletler
        public ICollection<Seat> Seats { get; set; }  // Uçuştaki koltuklar
    }
}