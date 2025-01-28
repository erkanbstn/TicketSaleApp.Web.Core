using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSaleApp.Models;
using TicketSaleApp.Models.Entity;

namespace TicketSaleApp.Controllers
{
    [Authorize] // Bu sınıftaki tüm aksiyonların yalnızca yetkilendirilmiş kullanıcılar tarafından erişilebilmesini sağlar
    public class UserController : Controller
    {
        private readonly AppDbContext _appDbContext;

        // Constructor, DbContext nesnesini alır ve kullanıma sunar
        public UserController(AppDbContext appDbContext)
        {
            this._appDbContext = appDbContext;
        }

        // Kullanıcıya ait biletleri görüntüleme işlemi
        public IActionResult MyTickets()
        {
            // Kullanıcıyı e-posta ile veritabanında sorgula
            var user = _appDbContext.Users.FirstOrDefault(u => u.Email == User.Identity.Name);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı."); // Kullanıcı bulunamazsa hata mesajı döndürülür
            }

            // Kullanıcıya ait biletleri sorgula
            var tickets = _appDbContext.Tickets
                .Where(t => t.UserId == user.UserId) // Kullanıcı ID'sine göre filtreleme yapılır
                .Include(t => t.Flight) // İlgili uçuş bilgileri eklenir
                .Include(t => t.Seat) // İlgili koltuk bilgileri eklenir
                .ToList();

            return View(tickets); // Bilet listesi, view'a gönderilir
        }

        // Bilet iptal işlemi
        public IActionResult CancelTicket(int ticketId)
        {
            var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Stored Procedure kullanarak bilet bilgilerini al
            var ticket = _appDbContext.Tickets
                .FromSqlRaw($"EXEC GetTicketByUserIdAndTicketId @TicketId = {ticketId}, @UserId = {userId}")
                .AsEnumerable()
                .FirstOrDefault();

            // Bilet bulunamazsa hata mesajı
            if (ticket == null)
            {
                ViewBag.Message = "Bilet bulunamadı veya bu bilet size ait değil.";
                return RedirectToAction("MyTickets");
            }

            // Eğer bilet zaten iptal edilmişse kullanıcıyı bilgilendir
            if (ticket.Status == TicketStatus.İptalEdildi)
            {
                ViewBag.Message = "Bu bilet zaten iptal edilmiştir.";
                return RedirectToAction("MyTickets");
            }

            // Biletin durumunu iptal olarak güncelle
            ticket.Status = TicketStatus.İptalEdildi;

            // Koltuk durumu da güncellenmeli (Koltuk tekrar boş olarak işaretlenebilir)
            var seat = _appDbContext.Seats
                .FirstOrDefault(s => s.SeatId == ticket.SeatId);
            if (seat != null)
            {
                seat.IsAvailable = true; // Koltuğu tekrar boş olarak işaretle
            }

            _appDbContext.SaveChanges(); // Değişiklikleri veritabanına kaydet

            return RedirectToAction("MyTickets"); // İptal işlemi sonrası tekrar MyTickets sayfasına yönlendir
        }

        // Kullanıcı profilini görüntüleme işlemi
        public IActionResult MyProfile()
        {
            var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Kullanıcıyı, ID'si ile sorgulamak için SQL fonksiyonu çağırılır
            var user = _appDbContext.Users.FromSqlRaw("SELECT * FROM dbo.GetUserById(@UserId)",
                                                       new SqlParameter("@UserId", userId))
                                           .FirstOrDefault();

            if (user == null)
                return NotFound();

            return View(user); // Kullanıcı bilgileri, View'a gönderilir
        }

        [HttpPost] // Post metodunda profil güncelleme işlemi
        public IActionResult MyProfile(User user)
        {
            var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Kullanıcı bilgilerini güncellemek için Stored Procedure kullanılır
            var parameters = new[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@FirstName", user.FirstName),
                new SqlParameter("@LastName", user.LastName),
                new SqlParameter("@Email", user.Email),
                new SqlParameter("@Password", user.Password)
            };

            _appDbContext.Database.ExecuteSqlRaw("EXEC UpdateUserProfile @UserId, @FirstName, @LastName, @Email, @Password", parameters);

            return RedirectToAction("MyProfile"); // Profil güncellenince kullanıcıyı MyProfile sayfasına yönlendir
        }

        // Yeni bilet oluşturma sayfası
        public IActionResult NewTicket()
        {
            var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Uçuşları veritabanından al
            var flights = _appDbContext.Flights.FromSqlRaw("SELECT * FROM FlightListView").ToList();

            // Uçuşları ViewData ile veya ViewModel ile aktarma
            ViewBag.Flights = new SelectList(flights, "FlightId", "FlightNumber");

            return View();
        }

        // Koltuk seçme işlemi
        [HttpGet]
        public IActionResult SelectSeat(int flightId)
        {
            // Seçilen uçuşu veritabanından al
            var flight = _appDbContext.Flights
            .FromSqlRaw("EXEC GetFlightById @FlightId = {0}", flightId)
            .FirstOrDefault();

            if (flight == null)
            {
                return NotFound();
            }

            // Bu uçuşa ait boş koltukları al
            var seats = _appDbContext.Seats
            .FromSqlRaw("EXEC GetAvailableSeatsByFlightId @FlightId = {0}", flightId)
            .ToList();

            // Uçuş bilgisi ve koltuklar ViewBag ile gönderilir
            ViewBag.FlightId = flightId;
            ViewBag.Seats = seats;

            return View();
        }

        // Yeni bilet oluşturma işlemi
        [HttpPost]
        public IActionResult CreateTicket(int flightId, string seatNumber)
        {
            var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Uçuşu ve koltuğu al
            var flight = _appDbContext.Flights
                .FirstOrDefault(f => f.FlightId == flightId);

            var seat = _appDbContext.Seats
                .FirstOrDefault(s => s.SeatNumber == seatNumber && s.FlightId == flightId);

            if (seat == null || !seat.IsAvailable)
            {
                // Koltuk dolu veya geçersizse hata mesajı ver
                ViewBag.Message = "Seçilen koltuk dolu veya geçersiz.";
                return RedirectToAction("NewTicket");
            }

            // Yeni bilet oluşturuluyor
            var ticket = new Ticket
            {
                UserId = userId,
                FlightId = flightId,
                SeatId = seat.SeatId,
                PurchaseDate = DateTime.Now,
                Status = TicketStatus.Beklemede // Başlangıçta biletin durumu beklemede
            };

            // Yeni bileti kaydet
            _appDbContext.Tickets.Add(ticket);

            // Koltuğu güncelle (koltuk artık dolu)
            seat.IsAvailable = false;

            _appDbContext.SaveChanges(); // Değişiklikleri kaydet

            return RedirectToAction("MyTickets"); // Bilet oluşturulduktan sonra MyTickets sayfasına yönlendir
        }

        // Belirli bir tarihe göre uçuşları almak için kullanılan metod
        public IActionResult GetFlightsByDate(DateTime selectedDate)
        {
            // Stored Procedure'ü çağırarak uçuşları al
            var flights = _appDbContext.Flights
            .FromSqlRaw("EXEC GetFlightsByDate @SelectedDate = {0}", selectedDate)
            .ToList();

            // Koltukları manuel olarak yükleyin
            foreach (var flight in flights)
            {
                flight.Seats = _appDbContext.Seats
                    .Where(s => s.FlightId == flight.FlightId)
                    .ToList();
            }

            return PartialView("_FlightListPartial", flights); // Uçuşlar PartialView ile döndürülür
        }
    }
}
