using Microsoft.AspNetCore.Mvc;
using TicketSaleApp.Models;
using TicketSaleApp.Models.Entity;

namespace TicketSaleApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context; // Veritabanı bağlamı (DbContext)

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // Kullanıcıları listeleme
        public IActionResult Index()
        {
            var users = _context.Users.Where(x => x.Status == true).ToList(); // Kullanıcıları veritabanından alır
            return View(users);
        }

        // Kullanıcıyı güncelleme formu
        public IActionResult Edit(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // Kullanıcı güncelleme işlemi
        [HttpPost]
        public IActionResult Edit(int id, User user)
        {
            user.Status = true;
            _context.Update(user); // Veritabanındaki kullanıcıyı günceller
            _context.SaveChanges(); // Değişiklikleri kaydeder
            return RedirectToAction(nameof(Index)); // Kullanıcıları listeleme sayfasına yönlendir
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        public IActionResult Create(User user)
        {
            // Kullanıcıyı veritabanına ekle
            _context.Users.Add(user);
            _context.SaveChanges();

            // Kullanıcı başarıyla oluşturulduğunda kullanıcıyı yönlendir
            return RedirectToAction(nameof(Index));
        }
    }
}
