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
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Salons.Add(salon);
                    context.SaveChanges();
                }

                // Ensure "desktop" user exists
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
                         IsEmailVerified = true
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

                // Ensure "mobile" user exists or update email
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
                         IsEmailVerified = true
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
                    // Update email if it changed
                    if (mobileUser.Email != "ademtolja123@gmail.com")
                    {
                        mobileUser.Email = "ademtolja123@gmail.com";
                        context.Users.Update(mobileUser);
                        context.SaveChanges();
                    }
                }

                // Ensure "barber" user exists
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
                         IsEmailVerified = true
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
                                Bio = "Expert Barber",
                                CreatedAt = DateTime.UtcNow
                        };
                        context.Barbers.Add(barberProfile);
                        context.SaveChanges();
                     }
                }

                // Separate check for WorkingHours to ensure they are added even if users exist
                if (!context.WorkingHours.Any())
                {
                    var barber = context.Barbers.FirstOrDefault();
                    if (barber != null)
                    {
                        var workingHoursList = new List<WorkingHours>();
                        for (int i = 1; i <= 5; i++)
                        {
                            workingHoursList.Add(new WorkingHours
                            {
                                BarberId = barber.Id,
                                DayOfWeek = i,
                                StartTime = new TimeSpan(9, 0, 0),
                                EndTime = new TimeSpan(17, 0, 0),
                                IsWorking = true,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        context.WorkingHours.AddRange(workingHoursList);
                        context.SaveChanges();
                    }
                }
            }
        }
    }
