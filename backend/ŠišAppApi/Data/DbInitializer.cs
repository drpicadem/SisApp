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
                context.Database.EnsureCreated();

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

                    if (!context.Users.Any())
                    {
                        var passwordHash = BCrypt.Net.BCrypt.HashPassword("test");

                        // 1. Desktop User (Admin)
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
                        context.SaveChanges(); // Save to get Id

                        var adminProfile = new Admin
                        {
                            UserId = adminUser.Id,
                            Role = "SuperAdmin",
                            IsSuperAdmin = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        context.Admins.Add(adminProfile);

                        // 2. Mobile User (Customer)
                        var mobileUser = new User
                        {
                            Username = "mobile",
                            Email = "mobile@sisapp.com",
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

                        // 3. Barber User (Owner) - For testing
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
        }
    }
}