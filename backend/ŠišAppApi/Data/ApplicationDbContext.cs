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
    public DbSet<Image> Images { get; set; }
    public DbSet<SalonAmenity> SalonAmenities { get; set; }
    public DbSet<BarberSpecialty> BarberSpecialties { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<FavoriteSalon> FavoriteSalons { get; set; }
    public DbSet<City> Cities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();


        modelBuilder.Entity<User>()
            .HasOne(u => u.Barber)
            .WithOne(b => b.User)
            .HasForeignKey<Barber>(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Admin)
            .WithOne(a => a.User)
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Customer)
            .WithOne(c => c.User)
            .HasForeignKey<Customer>(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(10, 2);
        modelBuilder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(10, 2);


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

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.UserId, a.ServiceId, a.AppointmentDateTime })
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled'");

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

        modelBuilder.Entity<City>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Salon>()
            .HasOne(s => s.CityRef)
            .WithMany(c => c.Salons)
            .HasForeignKey(s => s.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
    }
} 