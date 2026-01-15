using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Models;

namespace ŠišAppApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Barber> Barbers { get; set; }
    public DbSet<Salon> Salons { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Recommendation> Recommendations { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<WorkingHours> WorkingHours { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<AdminLog> AdminLogs { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<SalonAmenity> SalonAmenities { get; set; }
    public DbSet<BarberSpecialty> BarberSpecialties { get; set; }
    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Decimal precision
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(10, 2);
        modelBuilder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(10, 2);

        // Appointment relacije - sve na Restrict
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.User)
            .WithMany(u => u.Appointments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Service)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Barber)
            .WithMany(b => b.Appointments)
            .HasForeignKey(a => a.BarberId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Salon)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.SalonId)
            .OnDelete(DeleteBehavior.Restrict);
        // 1:1 Payment i Review
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Payment)
            .WithOne(p => p.Appointment)
            .HasForeignKey<Payment>(p => p.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Review)
            .WithOne(r => r.Appointment)
            .HasForeignKey<Review>(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review relacije - sve na Restrict
        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Barber)
            .WithMany(b => b.Reviews)
            .HasForeignKey(r => r.BarberId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Salon)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.SalonId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // BarberSpecialties konfiguracija
        modelBuilder.Entity<BarberSpecialty>()
            .HasOne(bs => bs.Barber)
            .WithMany(b => b.Specialties)
            .HasForeignKey(bs => bs.BarberId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BarberSpecialty>()
            .HasOne(bs => bs.Service)
            .WithMany(s => s.BarberSpecialties)
            .HasForeignKey(bs => bs.ServiceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
} 