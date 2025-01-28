using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSaleApp.Models;
using TicketSaleApp.Models.Entity;
namespace TicketSaleApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FlightController : Controller
    {
        private readonly AppDbContext _context;

        public FlightController(AppDbContext context)
        {
            _context = context;
        }

        // Uçuşları Listeleme
        public IActionResult Index()
        {
            var flights = _context.Flights.Include(f => f.Seats).ToList();
            return View(flights);
        }

        // Uçuş Ekleme Sayfası
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Flight flight)
        {
            _context.Flights.Add(flight);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // Uçuş Düzenleme Sayfası
        public IActionResult Edit(int id)
        {
            var flight = _context.Flights.Find(id);
            if (flight == null)
            {
                return NotFound();
            }
            return View(flight);
        }

        [HttpPost]
        public IActionResult Edit(int id, Flight flight)
        {
            try
            {
                _context.Update(flight);
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Flights.Any(f => f.FlightId == id))
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