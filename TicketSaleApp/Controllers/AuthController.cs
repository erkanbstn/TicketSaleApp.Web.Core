using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSaleApp.Models;
using TicketSaleApp.Models.DTO;
using TicketSaleApp.Models.Entity;

namespace TicketSaleApp.Controllers
{
    [AllowAnonymous] // Bu controller'a anonim kullanıcılar da erişebilir
    public class AuthController : Controller
    {
        private readonly AppDbContext _appDbContext;

        // Constructor: DbContext'in enjekte edilmesi
        public AuthController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        // Kullanıcıyı giriş sayfasına yönlendirir
        public IActionResult Login()
        {
            return View();
        }

        // POST: Kullanıcı giriş işlemi
        [HttpPost]
        public async Task<IActionResult> Login(UserLoginDTO userLoginDTO)
        {
            // Kullanıcının kimliğini doğrulamak için SQL fonksiyonu çağrılır
            var userId = _appDbContext.Users
                .FromSqlRaw("SELECT * FROM dbo.fn_GetUserByCredentials({0}, {1})", userLoginDTO.Email, userLoginDTO.Password)
                .FirstOrDefault()?.UserId;

            // Kullanıcıyı veritabanından al
            var user = _appDbContext.Users
                .FirstOrDefault(u => u.UserId == userId); // Şifre doğrulama henüz yapılmamış

            // Kullanıcı bulunamazsa hata mesajı göster
            if (user == null)
            {
                ViewBag.Message = "E-posta veya şifre hatalı!";
                return View();
            }

            // Kullanıcıya ait claim'ler oluşturuluyor
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email), // Kullanıcının e-posta adresi
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Kullanıcı ID'si
            };

            // Eğer kullanıcı admin değilse rol olarak "User", adminse "Admin" rolü eklenir
            if (user.Status == true)
                claims.Add(new Claim(ClaimTypes.Role, "User"));
            else
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            // ClaimsIdentity nesnesi oluşturuluyor
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // ClaimsPrincipal oluşturuluyor ve kullanıcı çerezle doğrulanıyor
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            // Kullanıcı "User" ise MyTickets sayfasına, "Admin" ise Admin sayfasına yönlendirilir
            if (user.Status == true) // Kullanıcı ise
                return RedirectToAction("MyTickets", "User"); // Kullanıcıyı biletler sayfasına yönlendir
            else // Admin ise
                return RedirectToAction("Index", "User", new { area = "Admin" }); // Admin ana sayfasına yönlendir
        }

        // Kullanıcı çıkış işlemi
        public async Task<IActionResult> LogOut()
        {
            // Kullanıcıyı oturumdan çıkarmak
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Login sayfasına yönlendir
            return RedirectToAction("Login", "Auth");
        }

        // Kullanıcı kayıt sayfası
        public IActionResult Register()
        {
            return View();
        }

        // POST: Kullanıcı kaydı
        [HttpPost]
        public async Task<IActionResult> Register(UserRegisterDTO userRegisterDTO)
        {
            // E-posta adresiyle kullanıcıyı veritabanında kontrol et
            var existingUser = _appDbContext.Users
                .FromSqlRaw("SELECT * FROM dbo.GetUserByEmail(@Email)", new SqlParameter("@Email", userRegisterDTO.Email))
                .FirstOrDefault();

            // Eğer kullanıcı zaten varsa hata mesajı göster
            if (existingUser != null)
            {
                ViewBag.Message = "Bu e-posta adresi zaten kayıtlı!";
                return View();
            }

            // Yeni kullanıcı oluşturuluyor
            var user = new User
            {
                Email = userRegisterDTO.Email,
                Password = userRegisterDTO.Password, // Şifreyi hashleyebilirsiniz (örneğin bcrypt ile)
                Status = true, // Varsayılan olarak aktif kullanıcı
            };

            // Kullanıcıyı veritabanına ekle
            _appDbContext.Users.Add(user);
            await _appDbContext.SaveChangesAsync();

            // Kayıt işlemi başarılıysa kullanıcıyı bilgilendir
            ViewBag.Message = "Kayıt başarılı, lütfen giriş yapın!";
            return View();
        }
    }
}
