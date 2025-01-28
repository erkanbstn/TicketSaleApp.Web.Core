using Microsoft.EntityFrameworkCore;
using TicketSaleApp.Models.Entity;

namespace TicketSaleApp.Models
{
    // Veritabanı başlangıç verilerini eklemek için kullanılan sınıf
    public static class DbInitializer
    {
        // Veritabanına başlangıç verilerini ekler
        public static void Seed(AppDbContext context)
        {
            // Eğer veritabanında zaten veri varsa, işlemi durdur
            if (context.Users.Any() || context.Flights.Any() || context.Seats.Any() || context.Tickets.Any())
            {
                return;
            }

            // Admin Kullanıcı
            var adminUser = new User
            {
                FirstName = "Admin",
                LastName = "Admin",
                Email = "admin@example.com",
                Password = "123", // Gerçek uygulamalarda hash kullanılmalıdır
                CreatedAt = DateTime.Now,
                Status = false,
            };
            context.Users.Add(adminUser);  // Admin kullanıcısını ekle
            context.SaveChanges();  // Veritabanına kaydet

            // Kullanıcılar (random)
            var users = Enumerable.Range(1, 10).Select(i => new User
            {
                FirstName = $"FirstName {i}",
                LastName = $"LastName {i}",
                Email = $"user{i}@example.com",
                Password = "123",  // Parola hashlenmeli
                CreatedAt = DateTime.Now.AddDays(-i),
                Status = true,
            }).ToList();
            context.Users.AddRange(users);  // Kullanıcıları ekle
            context.SaveChanges();  // Veritabanına kaydet

            // Uçuşlar (random)
            var flights = Enumerable.Range(1, 5).Select(i => new Flight
            {
                FlightNumber = $"THY{i:000}",
                DepartureCity = $"City {i}",
                ArrivalCity = $"City {i + 1}",
                DepartureTime = DateTime.Now.AddDays(i),
                ArrivalTime = DateTime.Now.AddDays(i).AddHours(2),
                AvailableSeats = 100 // Her uçuş için varsayılan 100 koltuk
            }).ToList();
            context.Flights.AddRange(flights);  // Uçuşları ekle
            context.SaveChanges();  // Veritabanına kaydet

            // Koltuklar (Her uçuş için koltuk ekleme)
            var seats = new List<Seat>();
            foreach (var flight in flights)
            {
                for (int i = 1; i <= 100; i++) // 100 koltuk
                {
                    seats.Add(new Seat
                    {
                        FlightId = flight.FlightId,
                        SeatNumber = $"{i / 10 + 1}{(char)('A' + i % 10)}", // Koltuk numarası örneği: 1A, 1B, 1C, ...
                        IsAvailable = true, // Koltuk başlangıçta mevcut
                    });
                }
            }
            context.Seats.AddRange(seats);  // Koltukları ekle
            context.SaveChanges();  // Veritabanına kaydet

            // Biletler (Her kullanıcıya rastgele uçuş ve koltuk atama)
            var tickets = new List<Ticket>();
            var random = new Random();

            foreach (var user in users)
            {
                // Rastgele bir uçuş seç
                var randomFlight = flights[random.Next(flights.Count)];

                // Seçilen uçuş için uygun bir koltuk bul
                var availableSeat = context.Seats.FirstOrDefault(s => s.FlightId == randomFlight.FlightId && s.IsAvailable);
                if (availableSeat != null)
                {
                    availableSeat.IsAvailable = false; // Koltuğu dolu olarak işaretle

                    // Bilet oluştur
                    var ticket = new Ticket
                    {
                        UserId = user.UserId,
                        FlightId = randomFlight.FlightId,
                        SeatId = availableSeat.SeatId,
                        PurchaseDate = DateTime.Now,
                        Status = TicketStatus.Beklemede, // Bilet durumu başlangıçta beklemede
                    };
                    tickets.Add(ticket);  // Bileti listeye ekle
                }
            }

            context.Tickets.AddRange(tickets);  // Biletleri ekle
            context.SaveChanges();  // Veritabanına kaydet

            // Kullanıcı eklediğinde CreatedAt kolonuna otomatik olarak bugünün tarihini atar
            var setCreatedAtTrigger = @"
                CREATE TRIGGER trg_Users_Insert
                ON Users
                AFTER INSERT
                AS
                BEGIN
                    UPDATE Users
                    SET CreatedAt = GETDATE()
                    WHERE UserId IN (SELECT UserId FROM inserted)
                END";

            // Trigger'ı çalıştır
            context.Database.ExecuteSqlRaw(setCreatedAtTrigger);

            // Koltuk durumu, bilet iptali durumunda güncellenir
            var triggerSql = @"
                CREATE OR ALTER TRIGGER trg_UpdateSeatOnTicketCancel
                ON dbo.Tickets
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Sadece 'İptalEdildi' durumundaki değişiklikleri işleyelim
                    UPDATE Seats
                    SET IsAvailable = 1  -- Koltuğu boş olarak işaretle
                    FROM Seats
                    INNER JOIN Inserted ON Seats.SeatId = Inserted.SeatId
                    INNER JOIN Deleted ON Inserted.TicketId = Deleted.TicketId
                    WHERE Inserted.Status = 2  -- Enum olarak 'İptalEdildi' durumu 2'ye eşit
                      AND Deleted.Status <> 2; -- Önceki durum 'İptalEdildi' değilse
                END;
            ";

            // Trigger'ı çalıştır
            context.Database.ExecuteSqlRaw(triggerSql);

            // Ticket eklendiğinde koltuk sayısını 1 azalt
            var createTriggerForInsert = @"
                CREATE TRIGGER trg_UpdateAvailableSeatsOnInsert
                ON dbo.Tickets
                AFTER INSERT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    -- Uçuşun AvailableSeats değerini bir azalt
                    UPDATE Flights
                    SET AvailableSeats = AvailableSeats - 1
                    WHERE FlightId IN (SELECT FlightId FROM Inserted);
                END;";

            // Trigger'ı çalıştır
            context.Database.ExecuteSqlRaw(createTriggerForInsert);

            // Kullanıcı giriş işlemi kontrol fonksiyonu oluşturur
            var checkUserFunctionSql = @"
                CREATE FUNCTION dbo.fn_GetUserByCredentials (@Email NVARCHAR(100), @Password NVARCHAR(100))
                RETURNS TABLE
                AS
                RETURN
                (
                    SELECT UserId, FirstName, LastName, Email, Password, CreatedAt, Status
                    FROM Users
                    WHERE Email = @Email AND Password = @Password
                )
            ";

            // Fonksiyonu çalıştır
            context.Database.ExecuteSqlRaw(checkUserFunctionSql);

            // Kullanıcı bilgisi almak için fonksiyon oluştur
            var checkUserDataFunctionSql = @"
                CREATE OR ALTER FUNCTION GetUserById (@UserId INT)
                RETURNS TABLE
                AS
                RETURN
                (
                    SELECT * FROM Users WHERE UserId = @UserId
                )
            ";

            // Fonksiyonu çalıştır
            context.Database.ExecuteSqlRaw(checkUserDataFunctionSql);

            // Kullanıcı email kontrolü yapan fonksiyon oluştur
            var checkUserByEmailFunctionSql = @"
                CREATE OR ALTER FUNCTION GetUserByEmail (@Email NVARCHAR(256))
                RETURNS TABLE
                AS
                RETURN
                (
                    SELECT * FROM Users WHERE Email = @Email
                )
            ";

            // Fonksiyonu çalıştır
            context.Database.ExecuteSqlRaw(checkUserByEmailFunctionSql);

            // FlightListView SQL View'ını oluşturur
            var createViewSql = @"
                CREATE VIEW FlightListView AS
                SELECT * FROM Flights;
            ";

            // View'ı çalıştır
            context.Database.ExecuteSqlRaw(createViewSql);

            // Kullanıcı bilgilerini güncellemek için Stored Procedure oluşturur
            var createSpSql = @"
                CREATE PROCEDURE UpdateUserProfile
                    @UserId INT,
                    @FirstName NVARCHAR(100),
                    @LastName NVARCHAR(100),
                    @Email NVARCHAR(100),
                    @Password NVARCHAR(100)
                AS
                BEGIN
                    SET NOCOUNT ON;
                    UPDATE Users
                    SET 
                        FirstName = @FirstName,
                        LastName = @LastName,
                        Email = @Email,
                        Password = @Password
                    WHERE UserId = @UserId;
                END";

            // Stored Procedure'u çalıştır
            context.Database.ExecuteSqlRaw(createSpSql);

            // Tarihe göre uçuşları getiren Stored Procedure oluşturur
            var getFlightsByDateSpSql = @"
                        CREATE PROCEDURE GetFlightsByDate
                @SelectedDate DATE
            AS
            BEGIN
                SELECT 
                    f.FlightId,
                    f.FlightNumber,
                    f.DepartureCity,
                    f.ArrivalCity,
                    f.DepartureTime,
                    f.ArrivalTime,
                    f.AvailableSeats
                FROM 
                    Flights f
                WHERE 
                    CAST(f.DepartureTime AS DATE) = @SelectedDate
                ORDER BY 
                    f.DepartureTime;
            END;";

            // Stored Procedure'u çalıştır
            context.Database.ExecuteSqlRaw(getFlightsByDateSpSql);

            // Uçuş Id'ye göre ilgili uçuşu getiren Stored Procedure oluşturur
            var createGetFlightByIdSpSql = @"
                CREATE PROCEDURE GetFlightById
                    @FlightId INT
                AS
                BEGIN
                    SELECT 
                        f.FlightId,
                        f.FlightNumber,
                        f.DepartureCity,
                        f.ArrivalCity,
                        f.DepartureTime,
                        f.ArrivalTime,
                        f.AvailableSeats
                    FROM 
                        Flights f
                    WHERE 
                        f.FlightId = @FlightId;
                END;";

            // Stored Procedure'u çalıştır
            context.Database.ExecuteSqlRaw(createGetFlightByIdSpSql);

            // Uçuş Id'ye göre ilgili açık koltukları getiren Stored Procedure oluşturur
            var createGetAvailableSeatsByFlightIdSpSql = @"
                CREATE PROCEDURE GetAvailableSeatsByFlightId
                    @FlightId INT
                AS
                BEGIN
                    SELECT 
                        SeatNumber
                    FROM 
                        Seats
                    WHERE 
                        FlightId = @FlightId AND IsAvailable = 1;
                END;";

            // Stored Procedure'u çalıştır
            context.Database.ExecuteSqlRaw(createGetAvailableSeatsByFlightIdSpSql);

            // Bileti iptal eder
            var cancelTicketSpSql = @"
                CREATE PROCEDURE GetTicketByUserIdAndTicketId
                @TicketId INT,
                @UserId INT
            AS
            BEGIN
                -- Set NOCOUNT ON, böylece prosedür ek veri setleri döndürmez
                SET NOCOUNT ON;

                -- Belirtilen kullanıcı ID'si ve bilet ID'sine göre bilet bilgilerini döndür
                SELECT 
                    t.TicketId,
                    t.UserId,
                    t.FlightId,
                    t.SeatId,
                    t.PurchaseDate,
                    t.Status
                FROM 
                    Tickets t
                WHERE 
                    t.TicketId = @TicketId AND t.UserId = @UserId;
            END;
            ";

            // Stored Procedure'u çalıştır
            context.Database.ExecuteSqlRaw(cancelTicketSpSql);
        }
    }
}
