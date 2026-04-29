using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Constants;
using ŠišAppApi.Models;
using System.Text.Json;

namespace ŠišAppApi.Data
{
    public static class DbInitializer
    {
        internal static readonly byte[] SeedImageBytes = Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQEAAAAAAAD/4QBCRXhpZgAATU0AKgAAAAgAAYdpAAQAAAABAAAAGgAAAAAAAkAAAAMAAAABAEQAAEABAAEAAAABAAAAAAAAAAAAAP/bAEMACwkJBwkJBwkJCQkLCQkJCQkJCwkLCwwLCwsMDRAMEQ4NDgwSGRIlGh0lHRkfHCkpFiU3NTYaKjI+LSkwGTshE//bAEMBBwgICwkLFQsLFSwdGR0sLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLCwsLP/AABEIAQoB2gMBIgACEQEDEQH/xAAfAAABBQEBAQEBAQAAAAAAAAAAAQIDBAUGBwgJCgv/xAC1EAACAQMDAgQDBQUEBAAAAX0BAgMABBEFEiExQQYTUWEHInEUMoGRoQgjQrHBFVLR8CQzYnKCCQoWFxgZGiUmJygpKjQ1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4eLj5OXm5+jp6vHy8/T19vf4+fr/xAAfAQADAQEBAQEBAQEBAAAAAAAAAQIDBAUGBwgJCgv/xAC1EQACAQIEBAMEBwUEBAABAncAAQIDEQQFITEGEkFRB2FxEyIygQgUQpGhscEJIzNS8BVictEKFiQ04SXxFxgZGiYnKCkqNTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqCg4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2dri4+Tl5ufo6ery8/T19vf4+fr/2gAMAwEAAhEDEQA/API6KKKRYUUUUxBRRRQAUUUUAFFFFABRRRSGFFFFABRRRQAUUUUCCiiigAooopgFFFFABRRRQAUUUUAFFFFIAooopgFFFFIYUUUUAFFFFABRRRQAUUUUAFFFFMQUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRSAKKKKYBRRRSAKKKKACiiigAooopgFFFFABRRRSC4UUUUAFFFFMAooooAKKKKACiiigAooooAKKKKQBRRRTAKKKKQBRRRQMKKKKACiiigAooooEFFFFMQUUUUDCiiigAooooAKKKKBBRRRQMKKKKACiiigAooooAKKKKQBRRRTBBRRRSGFFFFABRRRTEFFFFABRRRQAUUUUAFFFFABRRRQAUUUUCCiiigYlLRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUhhRRRTEFFFFABRRRQAUUUUCCiiigAooooGFFFFABRRRQAUUUUAFFJS0AFFFFABRRRSGFFFFABRRRQAUUUUAFFFFMQUUUUAFFFFABRRRSGFFFFMQUUUUAFFFFABRRRQAUUUUDCiiigQUUUUgCiiimAUUUUAFFFFABRRRQAUUUUgCiiimAUUUUAFFFFAgooooGFFFFABRRRQAUUUUAFFFFAgooooAKKKKBhRRRSGFFFFABRRRQIKKKKYBRRRQAUUUUAFFFFIYUUUUAFFFFABRRRQIKKKKBhRRRQIKKKKBhRRRQAUUUUxBRRRSAKKKKYBRRRSAKKKKYBRRRQAUUUUAFFFFAgooooAKKKKBhRRRQAUUUUAFFFFABRRRQAUUUUAFFFFIAooooGFFFFAgooooAKKKKBhRRRTAKKKKBBRRRSGFFFFABRRRQAUUUUAFFFFMQUUUUgCiiigYUUUUAFFGaKACiiigAooopiCiiigAooopDCiiimIKKKKACiiigQUUUUAFFFFAwooooAKKKKACiiigAooooAKKKKACiiikMKKKKACiiigAooooAKKKKACiiimIKKKKACiiikMKKKKACiiigAooooAKKKKACiiimIKKKKACiiikAUUcUUwCiiigAooooAKKKKQwooooEFFFFMAooooGFFFFAmFFFFABRRRSAKKKKYBRRRQAUUUUAFFFFABRRRQAUUUUhhRRRQAUUUUCQUUUUDCiiigAooopiCiiigAooopDCiiigAooooAKKKKBBRRRQMKKKKACiiigAooooAKKKKYgooooAKKKKQwooooAKKKKBBRRRQAUUUUwCiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooAKKKKQwooooBhRRRTEgooopDCiiigAooopiCiiikMKKKKBBRRRTAKKKKQwooopiCjmiikAUUUUDCiiigAooooAKKKKYgooopDCiiigQUUUUDCiiimIKKKKACiiigAooooAKKKKACiiigAooooAKKKKBBRRRQMKKKKACiiigAooopBcKKKKYBRRRQAUUUUhhRRRQAUUUUxBRRRQMKKKKBBRRRQAUUUUAFFFFIAooopjCiiikAUUUUCCiiimAUUUUAFFFFIYUUUUxBRRRSGFFFFABRRRTEFFFFAgooooGFFFFABRRRQAUUUUAFFFFABRRRQAUUUUCCiiigAooooGFFFFABRRRQDCiiikMKKKKACiiimIKKKKQwooooAKKKKYgooooAKKKKACiiikAUUUUDCiiigAooooAKKKKACiiigAooopiCiiikMKKKKYgooopAFFFFAwooooAKKKKYgooooAKKKKACiiigAooooAKKKKACiiigQUUUUigooopiCiiigAooopDCiiigAooopiCiiikMKKKKACiiigAooopiCiiigYUUUUhBRRRTAKKKKQwooooAKKKKACiiigAooooEFFFFAwooooEFFFFMAooopDCiiigAooooEFFFFMAooooAKKKKACiiigAooooAKKKKAFpKKKQ2FFFFMAooopCCiiigYUUUUAFFFFMQUUUUhhRRRQAUUUUAFFFFABRRRQAUUUUxBRRRSAKKKKYBRRRQAUUUUAFFFFIYUUUUAFFFFABRRRTEFFFFABRRRQAUUUUgCiiimAUUUUAFFFFABRRRQAUUUUAFFFFAgooooAKKKKRQUUUUAFFFFAkFFFFAwooooAKKKKBBRRRQAUUUUwCiiikMKKKKACiiigAooooAKKKKYmFFFFABRRRQIKKKKAuFFFFAwooooAKKKKQBRRRQAUUUUDCiiigAooopiCiiigAooooAKKKKACiiigAooooEFFFFAwooooEFFFFIoKKKKBBRRRQNBRRRQAUUtJQNhRRRTJCiiikMKKKKYgooopAFFFFAwooooAKKKKACiiigQUUUUDCiiimIKKKKACiiigAooooAKKKKACiiigAooopDCiiigQUUUUDCiiigAooopiCiiikMKKKKZIUUUUDCiiigAooooEFFFFIoKKKKBBS0lFAxaKSloGFJRRQJhRRRTAKKKKQBRRRQAUUUUAFFFFABRRRQAUUUUAFFFFABRRRQAUUUUyQooopDCiiimAUUUUhhRRRTEFFFFABRRRSAKKKKYBRRRSAKKKKACiiigYUUUUAFFFFMQUUUUAFFFFABRRRQAUVLc/8fF1/wBdpf8A0I1FSAKKKKYBRRRSAKKKKBhRRRQAUUUUwCiiikAUUetLQAlFFFABRRRQAUUUUCCiiigYUUUUAFFFFABRRRQIKKKKYBRRRQIKKKWgYlFLSUhhRRRQAUUUUAFFFFABRRRTEFFFFIYUUUUCCiiigYUUUUwCiiigQUUUUAFFFdLbf8e9r/1xi/8AQRQB/9k=");

        public static void Seed(IApplicationBuilder applicationBuilder)
        {
            using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();

                if (context == null)
                {
                    throw new InvalidOperationException("Unable to retrieve ApplicationDbContext");
                }

                try
                {
                    Seed(context);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error seeding database: {ex.Message}");
                    Console.Error.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                    Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }
        }

        public static void Seed(ApplicationDbContext context)
        {
            context.Database.Migrate();
            SeedCities(context);

            var sarajevoId = GetCityId(context, "Sarajevo");
            var zenicaId = GetCityId(context, "Zenica");
            var mostarId = GetCityId(context, "Mostar");

            var adminUser = EnsureUser(context, "desktop", "desktop@sisapp.com", "Admin", "User", AppRoles.Admin);
            var customerUser = EnsureUser(context, "mobile", "mobile@sisapp.com", "Mobile", "User", AppRoles.User);
            var barberUser1 = EnsureUser(context, "barber", "barber@sisapp.com", "Barber", "One", AppRoles.Barber);
            var barberUser2 = EnsureUser(context, "barber2", "barber2@sisapp.com", "Barber", "Two", AppRoles.Barber);

            EnsureAdminProfile(context, adminUser);
            EnsureCustomerProfile(context, customerUser);

            var salon1 = EnsureSalon(context, "Glavni Salon", sarajevoId, "Zmaja od Bosne 12", "71000", "+38761111111");
            var salon2 = EnsureSalon(context, "Urban Fade", zenicaId, "Titova 15", "72000", "+38762222222");
            var salon3 = EnsureSalon(context, "Mostar Style", mostarId, "Kralja Tvrtka 8", "88000", "+38763333333");

            var barber1 = EnsureBarberProfile(context, barberUser1, salon1, "Iskusni frizer za fade i beard styling.");
            var barber2 = EnsureBarberProfile(context, barberUser2, salon2, "Specijalista za moderni stil i dječije šišanje.");

            EnsureWorkingHours(context, barber1.Id);
            EnsureWorkingHours(context, barber2.Id);

            var catHaircut = EnsureServiceCategory(context, "Šišanje", "Klasično i moderno šišanje.");
            var catBeard = EnsureServiceCategory(context, "Brada", "Uređivanje i oblikovanje brade.");
            var catKids = EnsureServiceCategory(context, "Djeca", "Usluge za najmlađe.");
            var catCare = EnsureServiceCategory(context, "Njega", "Pranje i tretmani njege.");

            var svc1 = EnsureService(context, salon1.Id, "Muško šišanje", "Klasično mašinica + makaze.", 30, 20.0m, catHaircut.Id);
            var svc2 = EnsureService(context, salon1.Id, "Uređivanje brade", "Precizno oblikovanje brade.", 20, 12.0m, catBeard.Id);
            var svc3 = EnsureService(context, salon1.Id, "Pranje kose", "Pranje i lagani styling.", 15, 8.0m, catCare.Id);
            var svc4 = EnsureService(context, salon2.Id, "Skin fade", "Detaljan skin fade tretman.", 40, 28.0m, catHaircut.Id);
            var svc5 = EnsureService(context, salon2.Id, "Dječije šišanje", "Brzo i prilagođeno djeci.", 25, 15.0m, catKids.Id);
            var svc6 = EnsureService(context, salon3.Id, "All-in paket", "Šišanje + brada + pranje.", 60, 45.0m, catHaircut.Id, isPopular: true);

            EnsureBarberSpecialty(context, barber1.Id, svc1.Id, 5, true);
            EnsureBarberSpecialty(context, barber1.Id, svc2.Id, 4, false);
            EnsureBarberSpecialty(context, barber1.Id, svc3.Id, 3, false);
            EnsureBarberSpecialty(context, barber2.Id, svc4.Id, 5, true);
            EnsureBarberSpecialty(context, barber2.Id, svc5.Id, 4, false);
            EnsureBarberSpecialty(context, barber2.Id, svc6.Id, 4, false);

            EnsureSalonAmenity(context, salon1.Id, "WiFi", "Besplatan internet u salonu.");
            EnsureSalonAmenity(context, salon1.Id, "Parking", "Osiguran parking za klijente.");
            EnsureSalonAmenity(context, salon2.Id, "Kafa", "Besplatna kafa tokom tretmana.");
            EnsureSalonAmenity(context, salon3.Id, "Kartično plaćanje", "Plaćanje karticom dostupno.");

            var now = DateTime.UtcNow;
            var appCompleted = EnsureAppointment(context, customerUser.Id, barber1.Id, svc1.Id, salon1.Id, now.AddDays(-7), AppointmentStatuses.Completed, AppointmentPaymentStatuses.Paid);
            var appConfirmed = EnsureAppointment(context, customerUser.Id, barber1.Id, svc2.Id, salon1.Id, now.AddDays(2), AppointmentStatuses.Confirmed, AppointmentPaymentStatuses.Pending);
            var appPending = EnsureAppointment(context, customerUser.Id, barber2.Id, svc4.Id, salon2.Id, now.AddDays(5), AppointmentStatuses.Pending, AppointmentPaymentStatuses.Pending);
            var appCancelled = EnsureAppointment(context, customerUser.Id, barber2.Id, svc5.Id, salon2.Id, now.AddDays(-2), AppointmentStatuses.Cancelled, AppointmentPaymentStatuses.Pending);

            EnsurePayment(context, appCompleted.Id, customerUser.Id, svc1.Price, "Card", "SeedTXN-1001", PaymentStatuses.Completed);
            EnsurePayment(context, appConfirmed.Id, customerUser.Id, svc2.Price, "Card", "SeedTXN-1002", PaymentStatuses.Pending);

            EnsureReview(context, customerUser.Id, barber1.Id, appCompleted.Id, salon1.Id, 5, "Odlična usluga i prijatan ambijent.");
            EnsureReview(context, customerUser.Id, barber2.Id, appCancelled.Id, salon2.Id, 4, "Dobra usluga, termin pomjeren.");

            EnsureFavoriteSalon(context, customerUser.Id, salon1.Id);
            EnsureFavoriteSalon(context, customerUser.Id, salon2.Id);

            EnsureNotification(context, customerUser.Id, NotificationTypes.Appointment, "Imate potvrđen termin u naredna 2 dana.");
            EnsureNotification(context, customerUser.Id, NotificationTypes.Recommendation, "Dodane su nove preporuke usluga.");

            EnsureRecommendation(context, customerUser.Id, svc6.Id, "Popularno i slično vašim prethodnim uslugama", 86.5f);
            EnsureRecommendation(context, customerUser.Id, svc4.Id, "Često birate ovaj salon", 79.2f);

            EnsureAdminLog(context, adminUser.Id, "Seed", "System", "Runtime full seed izvršen.");

            var imgCustomer = EnsureSeedImage(context, "seed", "customer-profile.jpg", "profile", customerUser.Id, "User", "Seed korisnička profilna slika");
            var imgBarber = EnsureSeedImage(context, "barber", "barber-1.jpg", "profile", barber1.Id, "Barber", "Seed barber profilna slika");
            var imgSalon = EnsureSeedImage(context, "seed", "salon-cover.jpg", "salon", salon1.Id, "Salon", "Seed salon cover slika");
            var imgCategory = EnsureSeedImage(context, "seed", "category-haircut.jpg", "category", catHaircut.Id, "ServiceCategory", "Seed kategorija slika");
            var imgAmenity = EnsureSeedImage(context, "seed", "amenity-wifi.jpg", "amenity", salon1.Id, "SalonAmenity", "Seed amenity slika");
            var imgReview = EnsureSeedImage(context, "seed", "review-sample.jpg", "review", appCompleted.Id, "Review", "Seed review slika");

            if (customerUser.ImageId != imgCustomer.Id)
            {
                customerUser.ImageId = imgCustomer.Id;
            }
            if (barber1.ImageIds == null || !barber1.ImageIds.Contains(imgBarber.Id))
            {
                barber1.ImageIds = AddImageToJsonList(barber1.ImageIds, imgBarber.Id);
            }
            if (salon1.ImageIds == null || !salon1.ImageIds.Contains(imgSalon.Id))
            {
                salon1.ImageIds = AddImageToJsonList(salon1.ImageIds, imgSalon.Id);
            }
            if (catHaircut.ImageId != imgCategory.Id)
            {
                catHaircut.ImageId = imgCategory.Id;
            }

            var wifiAmenity = context.SalonAmenities.FirstOrDefault(x => x.SalonId == salon1.Id && x.Name == "WiFi");
            if (wifiAmenity != null && wifiAmenity.ImageId != imgAmenity.Id)
            {
                wifiAmenity.ImageId = imgAmenity.Id;
            }

            var review = context.Reviews.FirstOrDefault(r => r.AppointmentId == appCompleted.Id);
            if (review != null && (review.ImageIds == null || !review.ImageIds.Contains(imgReview.Id)))
            {
                review.ImageIds = AddImageToJsonList(review.ImageIds, imgReview.Id);
            }

            EnsureDefaultImagesForAllUsers(context);
            EnsureDefaultImagesForAllBarbers(context);
            EnsureDefaultImagesForAllSalons(context);

            RecalculateSalonAndBarberRatings(context);
            context.SaveChanges();
        }

        private static User EnsureUser(ApplicationDbContext context, string username, string email, string firstName, string lastName, string role)
        {
            var user = context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("test"),
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    IsActive = true,
                    IsEmailVerified = true,
                    IsPhoneVerified = false,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(user);
                context.SaveChanges();
            }
            return user;
        }

        private static void EnsureAdminProfile(ApplicationDbContext context, User adminUser)
        {
            if (!context.Admins.Any(a => a.UserId == adminUser.Id))
            {
                context.Admins.Add(new Admin
                {
                    UserId = adminUser.Id,
                    Role = AppRoles.Admin,
                    CreatedAt = DateTime.UtcNow
                });
                context.SaveChanges();
            }
        }

        private static void EnsureCustomerProfile(ApplicationDbContext context, User customerUser)
        {
            if (!context.Customers.Any(c => c.UserId == customerUser.Id))
            {
                context.Customers.Add(new Customer
                {
                    UserId = customerUser.Id,
                    CreatedAt = DateTime.UtcNow
                });
                context.SaveChanges();
            }
        }

        private static Salon EnsureSalon(ApplicationDbContext context, string name, int cityId, string address, string postalCode, string phone)
        {
            var salon = context.Salons.FirstOrDefault(s => s.Name == name);
            if (salon == null)
            {
                salon = new Salon
                {
                    Name = name,
                    Address = address,
                    CityId = cityId,
                    PostalCode = postalCode,
                    Phone = phone,
                    Email = $"{name.Replace(" ", "").ToLower()}@sisapp.com",
                    IsActive = true,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Salons.Add(salon);
                context.SaveChanges();
            }
            return salon;
        }

        private static Barber EnsureBarberProfile(ApplicationDbContext context, User barberUser, Salon salon, string bio)
        {
            var barber = context.Barbers.FirstOrDefault(b => b.UserId == barberUser.Id);
            if (barber == null)
            {
                barber = new Barber
                {
                    UserId = barberUser.Id,
                    SalonId = salon.Id,
                    Bio = bio,
                    IsAvailable = true,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Barbers.Add(barber);
                context.SaveChanges();
            }
            return barber;
        }

        private static void EnsureWorkingHours(ApplicationDbContext context, int barberId)
        {
            if (context.WorkingHours.Any(w => w.BarberId == barberId))
            {
                return;
            }

            var list = new List<WorkingHours>();
            for (int day = 1; day <= 5; day++)
            {
                list.Add(new WorkingHours
                {
                    BarberId = barberId,
                    DayOfWeek = day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    IsWorking = true,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            list.Add(new WorkingHours
            {
                BarberId = barberId,
                DayOfWeek = 6,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(14, 0, 0),
                IsWorking = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });
            list.Add(new WorkingHours
            {
                BarberId = barberId,
                DayOfWeek = 0,
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.Zero,
                IsWorking = false,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });

            context.WorkingHours.AddRange(list);
            context.SaveChanges();
        }

        private static ServiceCategory EnsureServiceCategory(ApplicationDbContext context, string name, string description)
        {
            var category = context.ServiceCategories.FirstOrDefault(c => c.Name == name);
            if (category == null)
            {
                category = new ServiceCategory
                {
                    Name = name,
                    Description = description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.ServiceCategories.Add(category);
                context.SaveChanges();
            }
            return category;
        }

        private static Service EnsureService(ApplicationDbContext context, int salonId, string name, string description, int duration, decimal price, int categoryId, bool isPopular = false)
        {
            var service = context.Services.FirstOrDefault(s => s.SalonId == salonId && s.Name == name && !s.IsDeleted);
            if (service == null)
            {
                service = new Service
                {
                    SalonId = salonId,
                    Name = name,
                    Description = description,
                    DurationMinutes = duration,
                    Price = price,
                    CategoryId = categoryId,
                    IsPopular = isPopular,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Services.Add(service);
                context.SaveChanges();
            }
            return service;
        }

        private static void EnsureBarberSpecialty(ApplicationDbContext context, int barberId, int serviceId, int expertiseLevel, bool isPrimary)
        {
            if (context.BarberSpecialties.Any(x => x.BarberId == barberId && x.ServiceId == serviceId))
            {
                return;
            }

            context.BarberSpecialties.Add(new BarberSpecialty
            {
                BarberId = barberId,
                ServiceId = serviceId,
                ExpertiseLevel = expertiseLevel,
                IsPrimary = isPrimary,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static void EnsureSalonAmenity(ApplicationDbContext context, int salonId, string name, string description)
        {
            if (context.SalonAmenities.Any(x => x.SalonId == salonId && x.Name == name && !x.IsDeleted))
            {
                return;
            }

            context.SalonAmenities.Add(new SalonAmenity
            {
                SalonId = salonId,
                Name = name,
                Description = description,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static Appointment EnsureAppointment(
            ApplicationDbContext context,
            int userId,
            int barberId,
            int serviceId,
            int salonId,
            DateTime dateTime,
            string status,
            string paymentStatus)
        {
            var appointment = context.Appointments.FirstOrDefault(a =>
                a.UserId == userId && a.BarberId == barberId && a.ServiceId == serviceId && a.AppointmentDateTime == dateTime);
            if (appointment == null)
            {
                appointment = new Appointment
                {
                    UserId = userId,
                    BarberId = barberId,
                    ServiceId = serviceId,
                    SalonId = salonId,
                    AppointmentDateTime = dateTime,
                    Status = status,
                    PaymentStatus = paymentStatus,
                    CreatedAt = DateTime.UtcNow
                };
                context.Appointments.Add(appointment);
                context.SaveChanges();
            }
            return appointment;
        }

        private static void EnsurePayment(ApplicationDbContext context, int appointmentId, int userId, decimal amount, string method, string txId, string status)
        {
            if (context.Payments.Any(p => p.AppointmentId == appointmentId))
            {
                return;
            }

            context.Payments.Add(new Payment
            {
                AppointmentId = appointmentId,
                UserId = userId,
                Amount = amount,
                Currency = "BAM",
                Method = method,
                TransactionId = txId,
                Status = status,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static void EnsureReview(
            ApplicationDbContext context,
            int userId,
            int barberId,
            int appointmentId,
            int salonId,
            int rating,
            string comment)
        {
            if (context.Reviews.Any(r => r.AppointmentId == appointmentId && !r.IsDeleted))
            {
                return;
            }

            var customerId = context.Customers.Where(c => c.UserId == userId).Select(c => (int?)c.Id).FirstOrDefault();
            context.Reviews.Add(new Review
            {
                UserId = userId,
                BarberId = barberId,
                AppointmentId = appointmentId,
                SalonId = salonId,
                CustomerId = customerId,
                Rating = rating,
                Comment = comment,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static void EnsureFavoriteSalon(ApplicationDbContext context, int userId, int salonId)
        {
            if (context.FavoriteSalons.Any(f => f.UserId == userId && f.SalonId == salonId))
            {
                return;
            }
            context.FavoriteSalons.Add(new FavoriteSalon
            {
                UserId = userId,
                SalonId = salonId,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static void EnsureNotification(ApplicationDbContext context, int userId, string type, string message)
        {
            if (context.Notifications.Any(n => n.UserId == userId && n.Type == type && n.Message == message && !n.IsDeleted))
            {
                return;
            }
            context.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = "Obavještenje",
                Message = message,
                IsRead = false,
                SentAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static void EnsureRecommendation(ApplicationDbContext context, int userId, int serviceId, string reason, float score)
        {
            if (context.Recommendations.Any(r => r.UserId == userId && r.RecommendedServiceId == serviceId && !r.IsDeleted))
            {
                return;
            }
            context.Recommendations.Add(new Recommendation
            {
                UserId = userId,
                RecommendedServiceId = serviceId,
                Reason = reason.Length > 100 ? reason[..100] : reason,
                RelevanceScore = score,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static void EnsureAdminLog(ApplicationDbContext context, int adminUserId, string action, string entityType, string notes)
        {
            var admin = context.Admins.FirstOrDefault(a => a.UserId == adminUserId);
            if (admin == null)
            {
                return;
            }

            if (context.AdminLogs.Any(l => l.AdminId == admin.Id && l.Action == action && l.EntityType == entityType))
            {
                return;
            }

            context.AdminLogs.Add(new AdminLog
            {
                AdminId = admin.Id,
                Action = action,
                EntityType = entityType,
                Notes = notes,
                IpAddress = "127.0.0.1",
                UserAgent = "DbInitializer",
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static Image EnsureSeedImage(
            ApplicationDbContext context,
            string folder,
            string fileName,
            string? imageType,
            int? entityId,
            string? entityType,
            string? altText)
        {
            var existing = context.Images.FirstOrDefault(i =>
                i.EntityId == entityId && i.EntityType == entityType && i.FileName == fileName && !i.IsDeleted);
            if (existing != null)
            {
                var touched = false;
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    touched = true;
                }
                if (string.IsNullOrWhiteSpace(existing.ContentType))
                {
                    existing.ContentType = "image/jpeg";
                    touched = true;
                }
                if (existing.FileSize <= 0)
                {
                    existing.FileSize = SeedImageBytes.Length;
                    touched = true;
                }
                if (existing.FileData == null || existing.FileData.Length == 0)
                {
                    existing.FileData = SeedImageBytes;
                    touched = true;
                }
                var expectedUrl = $"/api/Images/file/{existing.Id}";
                if (!string.Equals(existing.Url, expectedUrl, StringComparison.Ordinal))
                {
                    existing.Url = expectedUrl;
                    touched = true;
                }
                if (touched)
                {
                    existing.UpdatedAt = DateTime.UtcNow;
                    context.SaveChanges();
                }
                return existing;
            }

            var image = new Image
            {
                FileName = fileName,
                ContentType = "image/jpeg",
                FileSize = SeedImageBytes.Length,
                FileData = SeedImageBytes,
                Url = string.Empty,
                ImageType = imageType,
                EntityId = entityId,
                EntityType = entityType,
                AltText = altText,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            image.Url = $"/api/Images/file/{image.Id}";
            context.Images.Add(image);
            context.SaveChanges();
            return image;
        }

        private static string AddImageToJsonList(string? imageIdsJson, string imageId)
        {
            var list = ParseImageJsonList(imageIdsJson);
            if (!list.Contains(imageId))
            {
                list.Add(imageId);
            }
            return JsonSerializer.Serialize(list);
        }

        private static List<string> ParseImageJsonList(string? imageIdsJson)
        {
            if (string.IsNullOrWhiteSpace(imageIdsJson))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(imageIdsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void EnsureDefaultImagesForAllUsers(ApplicationDbContext context)
        {
            var users = context.Users.Where(u => !u.IsDeleted).ToList();
            foreach (var user in users)
            {
                if (!string.IsNullOrWhiteSpace(user.ImageId))
                {
                    continue;
                }

                var image = EnsureSeedImage(context, "seed", $"user-{user.Id}.jpg", "profile", user.Id, "User", "Default user slika");
                user.ImageId = image.Id;
            }
        }

        private static void EnsureDefaultImagesForAllBarbers(ApplicationDbContext context)
        {
            var barbers = context.Barbers.Where(b => !b.IsDeleted).ToList();
            foreach (var barber in barbers)
            {
                var existingIds = ParseImageJsonList(barber.ImageIds);
                if (existingIds.Count > 0)
                {
                    continue;
                }

                var image = EnsureSeedImage(context, "seed", $"barber-{barber.Id}.jpg", "profile", barber.Id, "Barber", "Default barber slika");
                barber.ImageIds = AddImageToJsonList(barber.ImageIds, image.Id);
            }
        }

        private static void EnsureDefaultImagesForAllSalons(ApplicationDbContext context)
        {
            var salons = context.Salons.Where(s => !s.IsDeleted).ToList();
            foreach (var salon in salons)
            {
                var existingIds = ParseImageJsonList(salon.ImageIds);
                if (existingIds.Count > 0)
                {
                    continue;
                }

                var image = EnsureSeedImage(context, "seed", $"salon-{salon.Id}.jpg", "salon", salon.Id, "Salon", "Default salon slika");
                salon.ImageIds = AddImageToJsonList(salon.ImageIds, image.Id);
            }
        }

        private static void RecalculateSalonAndBarberRatings(ApplicationDbContext context)
        {
            var salonRatings = context.Reviews
                .Where(r => !r.IsDeleted && r.SalonId.HasValue)
                .GroupBy(r => r.SalonId!.Value)
                .Select(g => new { SalonId = g.Key, Avg = g.Average(x => x.Rating), Count = g.Count() })
                .ToList();

            foreach (var item in salonRatings)
            {
                var salon = context.Salons.FirstOrDefault(s => s.Id == item.SalonId);
                if (salon != null)
                {
                    salon.Rating = Math.Round(item.Avg, 2);
                    salon.ReviewCount = item.Count;
                }
            }

            var barberRatings = context.Reviews
                .Where(r => !r.IsDeleted)
                .GroupBy(r => r.BarberId)
                .Select(g => new { BarberId = g.Key, Avg = g.Average(x => x.Rating), Count = g.Count() })
                .ToList();

            foreach (var item in barberRatings)
            {
                var barber = context.Barbers.FirstOrDefault(b => b.Id == item.BarberId);
                if (barber != null)
                {
                    barber.Rating = Math.Round(item.Avg, 2);
                    barber.ReviewCount = item.Count;
                }
            }
        }

        private static int GetCityId(ApplicationDbContext context, string cityName)
        {
            var id = context.Cities.Where(c => c.Name == cityName).Select(c => c.Id).FirstOrDefault();
            if (id <= 0)
            {
                throw new InvalidOperationException($"City '{cityName}' was not found during seeding.");
            }
            return id;
        }

        private static void SeedCities(ApplicationDbContext context)
        {
            var cityNames = new[]
            {
                "Banovići","Banja Luka","Bihać","Bijeljina","Bileća","Bosanski Brod","Bosanska Dubica","Bosanska Gradiška","Bosansko Grahovo","Bosanska Krupa","Bosanski Novi","Bosanski Petrovac","Bosanski Šamac","Bratunac","Brčko","Breza","Bugojno","Busovača","Bužim","Cazin","Čajniče","Čapljina","Čelić","Čelinac","Čitluk","Derventa","Doboj","Donji Vakuf","Drvar","Foča","Fojnica","Gacko","Glamoč","Goražde","Gornji Vakuf","Gračanica","Gradačac","Grude","Hadžići","Han-Pijesak","Livno","Ilijaš","Jablanica","Jajce","Kakanj","Kalesija","Kalinovik","Kiseljak","Kladanj","Ključ","Konjic","Kotor-Varoš","Kreševo","Kupres","Laktaši","Lopare","Lukavac","Ljubinje","Ljubuški","Maglaj","Modriča","Mostar","Mrkonjić-Grad","Neum","Nevesinje","Novi Travnik","Odžak","Olovo","Orašje","Pale","Posušje","Prijedor","Prnjavor","Prozor","Rogatica","Rudo","Sanski Most","Sarajevo","Skender-Vakuf","Sokolac","Srbac","Srebrenica","Srebrenik","Stolac","Šekovići","Šipovo","Široki Brijeg","Teslić","Tešanj","Tomislav-Grad","Travnik","Trebinje","Trnovo","Tuzla","Ugljevik","Vareš","Velika Kladuša","Visoko","Višegrad","Vitez","Vlasenica","Zavidovići","Zenica","Zvornik","Žepa","Žepče","Živinice"
            };

            var existing = context.Cities.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var toInsert = cityNames
                .Where(name => !existing.Contains(name))
                .Select(name => new City
                {
                    Name = name
                })
                .ToList();

            if (toInsert.Count > 0)
            {
                context.Cities.AddRange(toInsert);
                context.SaveChanges();
            }
        }
    }
}
