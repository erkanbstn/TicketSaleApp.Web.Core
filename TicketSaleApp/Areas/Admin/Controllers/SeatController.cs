using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSaleApp.Models.Entity;
using TicketSaleApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketSaleApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SeatController : Controller
    {
        private readonly AppDbContext _context;

        public SeatController(AppDbContext context)
        {
            _context = context;
        }

        // Koltukları Listeleme
        public IActionResult Index()
        {
            var seats = _context.Seats.Include(s => s.Flight).ToList(); // Koltukları uçuşla birlikte al
            return View(seats);
        }

        // Koltuk Ekleme Sayfası
        public IActionResult Create()
        {
            ViewBag.Flights = _context.Flights.ToList();  // Uçuşları listeye aktar
            return View();
        }

        [HttpPost]
        public IActionResult Create(Seat seat)
        {
            _context.Seats.Add(seat);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // Koltuk Düzenleme Sayfası
        public IActionResult Edit(int id)
        {
            var seat = _context.Seats.FirstOrDefault(s => s.SeatId == id);

            if (seat == null)
            {
                return NotFound();
            }

            // Flight'ları ViewBag ile geçiyoruz
            var flights = _context.Flights.ToList();
            ViewBag.Flights = new SelectList(flights, "FlightId", "FlightNumber");

            // IsAvailable için true/false seçeneklerini ekliyoruz
            ViewBag.IsAvailable = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Text = "Boş", Value = "true" },
                new SelectListItem { Text = "Dolu", Value = "false" }
            }, "Value", "Text", seat.IsAvailable.ToString().ToLower());

            return View(seat);
        }

        [HttpPost]
        public IActionResult Edit(int id, Seat seat)
        {
            try
            {
                _context.Update(seat);
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Seats.Any(s => s.SeatId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
