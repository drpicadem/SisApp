using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Models;

namespace ŠišAppApi.Data
{
    public static class DbInitializer
    {
        public static void Seed(IApplicationBuilder applicationBuilder)
        {
            using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();

                if (context == null)
                {
                    throw new Exception("Unable to retrieve ApplicationDbContext");
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
                // Apply migrations automatically
                // Use EnsureCreated to match DB to current Code Model (bypassing old migrations)
                context.Database.Migrate();

                // 1. Ensure "Glavni Salon" exists
                if (!context.Salons.Any())
                {
                    var salon = new Salon
                    {
                        Name = "Glavni Salon",
                        Address = "Trg Alije Izetbegovića 1",
                        City = "Zenica",
                        PostalCode = "72000",
                        Country = "BiH",
                        Phone = "+38761123456",
                        Email = "info@sisapp.com",
                        Rating = 5.0,
                        IsActive = true,
                        IsVerified = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Salons.Add(salon);
                    context.SaveChanges();
                }

                // 2. Ensure "desktop" admin user exists
                if (!context.Users.Any(u => u.Username == "desktop"))
                {
                     var passwordHash = BCrypt.Net.BCrypt.HashPassword("test");
                     var adminUser = new User
                     {
                         Username = "desktop",
                         Email = "desktop@sisapp.com",
                         PasswordHash = passwordHash,
                         FirstName = "Admin",
                         LastName = "User",
                         Role = "Admin",
                         IsActive = true,
                         CreatedAt = DateTime.UtcNow, 
                         IsEmailVerified = true,
                         IsPhoneVerified = false
                     };
                     context.Users.Add(adminUser);
                     context.SaveChanges();

                     var adminProfile = new Admin
                     {
                         UserId = adminUser.Id,
                         Role = "SuperAdmin",
                         IsSuperAdmin = true,
                         CreatedAt = DateTime.UtcNow
                     };
                     context.Admins.Add(adminProfile);
                     context.SaveChanges();
                }

                // 3. Ensure "mobile" customer user exists
                var mobileUser = context.Users.FirstOrDefault(u => u.Username == "mobile");
                if (mobileUser == null)
                {
                     var passwordHash = BCrypt.Net.BCrypt.HashPassword("test");
                     mobileUser = new User
                     {
                         Username = "mobile",
                         Email = "ademtolja123@gmail.com",
                         PasswordHash = passwordHash,
                         FirstName = "Mobile",
                         LastName = "User",
                         Role = "User",
                         IsActive = true,
                         CreatedAt = DateTime.UtcNow,
                         IsEmailVerified = true,
                         IsPhoneVerified = false
                     };
                     context.Users.Add(mobileUser);
                     context.SaveChanges();

                     var customerProfile = new Customer
                     {
                         UserId = mobileUser.Id,
                         CreatedAt = DateTime.UtcNow
                     };
                     context.Customers.Add(customerProfile);
                     context.SaveChanges();
                }
                else
                {
                    if (mobileUser.Email != "ademtolja123@gmail.com")
                    {
                        mobileUser.Email = "ademtolja123@gmail.com";
                        context.Users.Update(mobileUser);
                        context.SaveChanges();
                    }
                }

                // 4. Ensure "barber" user exists
                if (!context.Users.Any(u => u.Username == "barber"))
                {
                     var passwordHash = BCrypt.Net.BCrypt.HashPassword("test");
                     var barberUser = new User
                     {
                         Username = "barber",
                         Email = "barber@sisapp.com",
                         PasswordHash = passwordHash,
                         FirstName = "Barber",
                         LastName = "User",
                         Role = "Barber",
                         IsActive = true,
                         CreatedAt = DateTime.UtcNow,
                         IsEmailVerified = true,
                         IsPhoneVerified = false
                     };
                     context.Users.Add(barberUser);
                     context.SaveChanges();

                     var salon = context.Salons.FirstOrDefault();
                     if (salon != null)
                     {
                        var barberProfile = new Barber
                        {
                                UserId = barberUser.Id,
                                SalonId = salon.Id,
                                Bio = "Iskusni frizer",
                                Rating = 5.0,
                                ReviewCount = 0,
                                AppointmentCount = 0,
                                IsAvailable = true,
                                IsVerified = true,
                                CreatedAt = DateTime.UtcNow
                        };
                        context.Barbers.Add(barberProfile);
                        context.SaveChanges();
                     }
                }

                // 5. Ensure WorkingHours exist
                if (!context.WorkingHours.Any())
                {
                    var barber = context.Barbers.FirstOrDefault();
                    if (barber != null)
                    {
                        var workingHoursList = new List<WorkingHours>
                        {
                            new WorkingHours { BarberId = barber.Id, DayOfWeek = 1, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsWorking = true, IsDefault = false, CreatedAt = DateTime.UtcNow },
                            new WorkingHours { BarberId = barber.Id, DayOfWeek = 2, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsWorking = true, IsDefault = false, CreatedAt = DateTime.UtcNow },
                            new WorkingHours { BarberId = barber.Id, DayOfWeek = 3, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsWorking = true, IsDefault = false, CreatedAt = DateTime.UtcNow },
                            new WorkingHours { BarberId = barber.Id, DayOfWeek = 4, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsWorking = true, IsDefault = false, CreatedAt = DateTime.UtcNow },
                            new WorkingHours { BarberId = barber.Id, DayOfWeek = 5, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsWorking = true, IsDefault = false, CreatedAt = DateTime.UtcNow },
                            new WorkingHours { BarberId = barber.Id, DayOfWeek = 6, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(14, 0, 0), IsWorking = true, IsDefault = false, CreatedAt = DateTime.UtcNow },
                            new WorkingHours { BarberId = barber.Id, DayOfWeek = 0, StartTime = new TimeSpan(0, 0, 0), EndTime = new TimeSpan(0, 0, 0), IsWorking = false, IsDefault = false, CreatedAt = DateTime.UtcNow }
                        };
                        context.WorkingHours.AddRange(workingHoursList);
                        context.SaveChanges();
                    }
                }

                // 6. Ensure Services exist
                if (!context.Services.Any())
                {
                    var salon = context.Salons.FirstOrDefault();
                    if (salon != null)
                    {
                        var servicesList = new List<Service>
                        {
                            new Service { SalonId = salon.Id, Name = "Muško šišanje", Description = "Klasično muško šišanje mašinicom i makazama.", DurationMinutes = 30, Price = 15.00m, IsPopular = false, DisplayOrder = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                            new Service { SalonId = salon.Id, Name = "Uređivanje brade", Description = "Oblikovanje i trimanje brade sa prelivima.", DurationMinutes = 20, Price = 10.00m, IsPopular = false, DisplayOrder = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                            new Service { SalonId = salon.Id, Name = "Dječije šišanje", Description = "Šišanje za dječake prilagođeno uzrastu.", DurationMinutes = 30, Price = 12.00m, IsPopular = false, DisplayOrder = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                            new Service { SalonId = salon.Id, Name = "Pranje kose", Description = "Opuštajuće pranje i sušenje kose uz masažu glave.", DurationMinutes = 10, Price = 5.00m, IsPopular = false, DisplayOrder = 0, IsActive = true, CreatedAt = DateTime.UtcNow }
                        };
                        context.Services.AddRange(servicesList);
                        context.SaveChanges();
                    }
                }

                // 7. Ensure BarberSpecialties exist
                if (!context.BarberSpecialties.Any())
                {
                    var barber = context.Barbers.FirstOrDefault();
                    if (barber != null && context.Services.Any())
                    {
                        var services = context.Services.Take(4).ToList();
                        if (services.Count == 4) 
                        {
                            var specialties = new List<BarberSpecialty>
                            {
                                new BarberSpecialty { BarberId = barber.Id, ServiceId = services[0].Id, ExpertiseLevel = 5, IsPrimary = true, CreatedAt = DateTime.UtcNow },
                                new BarberSpecialty { BarberId = barber.Id, ServiceId = services[1].Id, ExpertiseLevel = 4, IsPrimary = false, CreatedAt = DateTime.UtcNow },
                                new BarberSpecialty { BarberId = barber.Id, ServiceId = services[2].Id, ExpertiseLevel = 4, IsPrimary = false, CreatedAt = DateTime.UtcNow },
                                new BarberSpecialty { BarberId = barber.Id, ServiceId = services[3].Id, ExpertiseLevel = 3, IsPrimary = false, CreatedAt = DateTime.UtcNow }
                            };
                            context.BarberSpecialties.AddRange(specialties);
                            context.SaveChanges();
                        }
                    }
                }
            }
        }
    }
