using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketSaleApp.Models.Entity;
using TicketSaleApp.Models;

namespace TicketSaleApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;

        public TicketController(AppDbContext context)
        {
            _context = context;
        }

        // Tüm biletleri listeleme
        public IActionResult Index()
        {
            var tickets = _context.Tickets
                .Include(t => t.Flight)
                .Include(t => t.Seat)
                .Include(t => t.User) // User ilişkisinin dahil edilmesi
                .ToList();

            return View(tickets);
        }

        // Yeni bilet ekleme formu
        public IActionResult Create()
        {
            ViewBag.Flights = new SelectList(_context.Flights, "FlightId", "FlightNumber");
            ViewBag.Seats = new SelectList(_context.Seats, "SeatId", "SeatNumber");
            ViewBag.Users = new SelectList(
                _context.Users.Where(x => x.Status == true).Select(u => new
                {
                    UserId = u.UserId,
                    FullName = $"{u.FirstName} {u.LastName}"
                }),
                "UserId",
                "FullName"
            );
            ViewBag.Statuses = new SelectList(
                Enum.GetValues(typeof(TicketStatus))
                    .Cast<TicketStatus>()
                    .Select(s => new { Value = ((int)s).ToString(), Text = s.ToString() }),
                "Value",
                "Text"
            );

            return View();
        }

        // Yeni bilet ekleme işlemi
        [HttpPost]
        public IActionResult Create(Ticket ticket)
        {
            ticket.PurchaseDate = DateTime.Now;
            _context.Tickets.Add(ticket);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // Bilet düzenleme formu
        public IActionResult Edit(int id)
        {
            var ticket = _context.Tickets.Find(id);
            if (ticket == null) return NotFound();

            ViewBag.Flights = new SelectList(_context.Flights, "FlightId", "FlightNumber");
            ViewBag.Seats = new SelectList(_context.Seats, "SeatId", "SeatNumber");
            ViewBag.Statuses = Enum.GetValues(typeof(TicketStatus))
                                   .Cast<TicketStatus>()
                                   .Select(s => new SelectListItem
                                   {
                                       Text = s.ToString(),
                                       Value = ((int)s).ToString()
                                   }).ToList();

            return View(ticket);
        }

        // Bilet düzenleme işlemi
        [HttpPost]
        public IActionResult Edit(int id, Ticket ticket)
        {
            try
            {
                _context.Update(ticket);
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tickets.Any(t => t.TicketId == ticket.TicketId))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
