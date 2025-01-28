using Microsoft.EntityFrameworkCore;
using TicketSaleApp.Models.Entity;

namespace TicketSaleApp.Models
{
    // Uygulamanın veritabanı bağlamını temsil eden sınıf
    public class AppDbContext : DbContext
    {
        // DbContextOptions parametresi ile yapılandırma sağlanır
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Veritabanındaki tabloları DbSet olarak tanımlar
        public DbSet<User> Users { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        // Model yapılandırmalarının yer aldığı metot
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tablo yapılandırmalarını özelleştir
            modelBuilder.Entity<User>()
                .ToTable(tb => tb.UseSqlOutputClause(false)); // Kullanıcı tablosu için SQL çıkışlarını devre dışı bırak

            modelBuilder.Entity<Seat>()
                .ToTable(tb => tb.UseSqlOutputClause(false)); // Koltuk tablosu için SQL çıkışlarını devre dışı bırak

            modelBuilder.Entity<Ticket>()
                .ToTable(tb => tb.UseSqlOutputClause(false)); // Bilet tablosu için SQL çıkışlarını devre dışı bırak

            modelBuilder.Entity<Flight>()
                .ToTable(tb => tb.UseSqlOutputClause(false)); // Uçuş tablosu için SQL çıkışlarını devre dışı bırak

            // Kullanıcı ve Bilet arasındaki ilişkiyi (1-çok) yapılandırma
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User) // Her bilet bir kullanıcıya ait
                .WithMany(u => u.Tickets) // Her kullanıcı birden fazla bilet alabilir
                .HasForeignKey(t => t.UserId) // Biletler, kullanıcıyı UserId ile referans alır
                .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silindiğinde biletler de silinir

            // Uçuş ve Bilet arasındaki ilişkiyi (1-çok) yapılandırma
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Flight) // Her bilet bir uçuşa ait
                .WithMany(f => f.Tickets) // Her uçuş birden fazla bilet alabilir
                .HasForeignKey(t => t.FlightId) // Biletler, uçuşu FlightId ile referans alır
                .OnDelete(DeleteBehavior.Restrict); // Bilet silindiğinde uçuşu etkilemez

            // Koltuk ve Bilet arasındaki ilişkiyi (1-çok) yapılandırma
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Seat) // Her bilet bir koltuğa ait
                .WithMany(s => s.Tickets) // Her koltuk birden fazla bilet alabilir
                .HasForeignKey(t => t.SeatId) // Biletler, koltuğu SeatId ile referans alır
                .OnDelete(DeleteBehavior.Restrict); // Koltuk silindiğinde biletleri etkilemez

            // Uçuş ve Koltuk arasındaki ilişkiyi (1-çok) yapılandırma
            modelBuilder.Entity<Seat>()
                .HasOne(s => s.Flight) // Her koltuk bir uçuşa ait
                .WithMany(f => f.Seats) // Her uçuş birden fazla koltuk içerebilir
                .HasForeignKey(s => s.FlightId) // Koltuklar, uçuşu FlightId ile referans alır
                .OnDelete(DeleteBehavior.Cascade); // Uçuş silindiğinde koltuklar da silinir

            // TicketStatus enum'ının integer olarak saklanması
            modelBuilder.Entity<Ticket>()
                .Property(t => t.Status)
                .HasConversion<int>(); // TicketStatus enum'ını integer değeri olarak veritabanına kaydeder
        }

        // Veritabanının oluşturulmasını sağlar (Eğer veritabanı mevcut değilse)
        public void EnsureDatabaseCreated()
        {
            this.Database.EnsureCreated(); // Veritabanını oluşturur, eğer yoksa
        }
    }
}
