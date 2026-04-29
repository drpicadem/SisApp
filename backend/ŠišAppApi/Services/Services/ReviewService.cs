using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAdminLogService _adminLogService;

        public ReviewService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAdminLogService adminLogService)
        {
            _context = context;
            _notificationService = notificationService;
            _adminLogService = adminLogService;
        }

        private IQueryable<Review> IncludeAll() =>
            _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Barber).ThenInclude(b => b.User)
                .Include(r => r.Salon)
                .Include(r => r.Appointment).ThenInclude(a => a.Service);

        private static ReviewDto MapToDto(Review r) => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Anoniman korisnik",
            BarberId = r.BarberId,
            BarberName = r.Barber?.User != null ? $"{r.Barber.User.FirstName} {r.Barber.User.LastName}" : "Nepoznato",
            SalonId = r.SalonId,
            SalonName = r.Salon?.Name,
            AppointmentId = r.AppointmentId,
            ServiceName = r.Appointment?.Service?.Name,
            Rating = r.Rating,
            Comment = r.Comment ?? string.Empty,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            HelpfulCount = r.HelpfulCount,
            IsVerified = r.IsVerified,
            BarberResponse = r.BarberResponse,
            BarberRespondedAt = r.BarberRespondedAt
        };

        private static (int page, int pageSize) Normalize(int page, int pageSize)
        {
            var p = page < 1 ? 1 : page;
            var ps = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
            return (p, ps);
        }

        public async Task<ReviewDto> CreateReviewAsync(int userId, CreateReviewDto dto)
        {
            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.AppointmentId == dto.AppointmentId && r.UserId == userId);
            if (existing != null)
                throw new UserException("Recenzija već postoji za ovu rezervaciju.");

            var appointment = await _context.Appointments
                .Include(a => a.Barber)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

            if (appointment == null)
                throw new UserException("Termin nije pronađen.");
            if (appointment.Status != AppointmentStatuses.Completed)
                throw new UserException("Možete ostaviti recenziju samo za završene termine.");
            if (appointment.UserId != userId)
                throw new UserException($"Možete ostaviti recenziju samo za svoje termine. (TokenID={userId}, AppID={appointment.UserId})");
            if (appointment.BarberId != dto.BarberId)
                throw new UserException("Navedeni frizer nije radio ovaj termin.");

            var review = new Review
            {
                AppointmentId = dto.AppointmentId,
                UserId = userId,
                BarberId = dto.BarberId,
                SalonId = appointment.SalonId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                IsVerified = false,
                IsHidden = false,
                HelpfulCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            var barber = await _context.Barbers.FindAsync(dto.BarberId);
            if (barber != null)
            {
                var count = await _context.Reviews.Where(r => r.BarberId == dto.BarberId).CountAsync();
                var sum = await _context.Reviews.Where(r => r.BarberId == dto.BarberId).SumAsync(r => (double?)r.Rating) ?? 0d;
                barber.Rating = (sum + dto.Rating) / (count + 1);
                barber.ReviewCount = count + 1;
            }

            var salon = await _context.Salons.FindAsync(appointment.SalonId);
            if (salon != null)
            {
                var count = await _context.Reviews.Where(r => r.SalonId == appointment.SalonId).CountAsync();
                var sum = await _context.Reviews.Where(r => r.SalonId == appointment.SalonId).SumAsync(r => (double?)r.Rating) ?? 0d;
                salon.Rating = (sum + dto.Rating) / (count + 1);
            }

            await _context.SaveChangesAsync();

            var created = await IncludeAll().FirstOrDefaultAsync(r => r.Id == review.Id);
            return MapToDto(created!);
        }

        public async Task<ReviewDto> UpdateReviewAsync(int id, int userId, CreateReviewDto dto)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
                throw new UserException("Recenzija nije pronađena.");
            if (review.UserId != userId)
                throw new UserException("Možete ažurirati samo svoje recenzije.");

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;
            review.UpdatedAt = DateTime.UtcNow;

            var barber = await _context.Barbers.FindAsync(review.BarberId);
            if (barber != null)
            {
                var count = await _context.Reviews.Where(r => r.BarberId == review.BarberId).CountAsync();
                var sumExcl = await _context.Reviews.Where(r => r.BarberId == review.BarberId && r.Id != id).SumAsync(r => (double?)r.Rating) ?? 0d;
                barber.Rating = (sumExcl + dto.Rating) / count;
                barber.ReviewCount = count;
            }

            if (review.SalonId.HasValue)
            {
                var salon = await _context.Salons.FindAsync(review.SalonId);
                if (salon != null)
                {
                    var count = await _context.Reviews.Where(r => r.SalonId == review.SalonId).CountAsync();
                    var sumExcl = await _context.Reviews.Where(r => r.SalonId == review.SalonId && r.Id != id).SumAsync(r => (double?)r.Rating) ?? 0d;
                    salon.Rating = (sumExcl + dto.Rating) / count;
                }
            }

            await _context.SaveChangesAsync();
            var updated = await IncludeAll().FirstOrDefaultAsync(r => r.Id == id);
            return MapToDto(updated!);
        }

        public async Task<IEnumerable<ReviewDto>> GetMyReviewsAsync(int userId, int page, int pageSize)
        {
            var (p, ps) = Normalize(page, pageSize);
            var reviews = await IncludeAll()
                .Where(r => r.UserId == userId && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((p - 1) * ps).Take(ps)
                .ToListAsync();
            return reviews.Select(MapToDto);
        }

        public async Task<IEnumerable<ReviewDto>> GetMyBarberReviewsAsync(int userId, int page, int pageSize)
        {
            var (p, ps) = Normalize(page, pageSize);
            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
            if (barber == null)
                throw new UserException("Niste registrirani kao frizer.");

            var reviews = await IncludeAll()
                .Where(r => r.BarberId == barber.Id && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((p - 1) * ps).Take(ps)
                .ToListAsync();
            return reviews.Select(MapToDto);
        }

        public async Task<ReviewDto> RespondToReviewAsync(int id, int userId, string response)
        {
            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
            if (barber == null)
                throw new UserException("Niste registrirani kao frizer.");

            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
                throw new UserException("Recenzija nije pronađena.");
            if (review.BarberId != barber.Id)
                throw new UserException("Možete odgovoriti samo na svoje recenzije.");

            review.BarberResponse = response;
            review.BarberRespondedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var updated = await IncludeAll().FirstOrDefaultAsync(r => r.Id == id);
            return MapToDto(updated!);
        }

        public async Task<IEnumerable<ReviewDto>> GetReviewsForBarberAsync(int barberId, int page, int pageSize)
        {
            var (p, ps) = Normalize(page, pageSize);
            var reviews = await IncludeAll()
                .Where(r => r.BarberId == barberId && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((p - 1) * ps).Take(ps)
                .ToListAsync();
            return reviews.Select(MapToDto);
        }

        public async Task<IEnumerable<ReviewDto>> GetReviewsForSalonAsync(int salonId, int page, int pageSize)
        {
            var (p, ps) = Normalize(page, pageSize);
            var reviews = await IncludeAll()
                .Where(r => r.SalonId == salonId && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((p - 1) * ps).Take(ps)
                .ToListAsync();
            return reviews.Select(MapToDto);
        }

        public async Task MarkAsHelpfulAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                throw new NotFoundException("Recenzija nije pronađena.");

            review.HelpfulCount++;
            await _context.SaveChangesAsync();
        }

        public async Task VerifyReviewAsync(int id, int adminUserId, string? ipAddress, string? userAgent)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                throw new NotFoundException("Recenzija nije pronađena.");
            if (review.IsVerified)
                throw new UserException("Recenzija je već verificirana.");

            review.IsVerified = true;
            review.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _adminLogService.LogAsync(
                adminUserId, "ReviewVerified", "Review", review.Id,
                $"Admin verificirao recenziju {review.Id}.",
                ipAddress, userAgent);

            await _notificationService.CreateNotification(
                review.UserId,
                $"Vaša recenzija (ID: {review.Id}) je odobrena.",
                NotificationTypes.Appointment,
                review.Id.ToString());
        }

        public async Task HideReviewAsync(int id, int adminUserId, string reason, string? ipAddress, string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new UserException("Razlog odbijanja je obavezan.");

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                throw new NotFoundException("Recenzija nije pronađena.");
            if (review.IsHidden)
                throw new UserException("Recenzija je već sakrivena.");

            review.IsHidden = true;
            review.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var trimmedReason = reason.Trim();

            await _adminLogService.LogAsync(
                adminUserId, "ReviewRejected", "Review", review.Id,
                $"Admin odbio recenziju {review.Id}. Razlog: {trimmedReason}",
                ipAddress, userAgent);

            await _notificationService.CreateNotification(
                review.UserId,
                $"Vaša recenzija (ID: {review.Id}) je odbijena. Razlog: {trimmedReason}",
                NotificationTypes.Appointment,
                review.Id.ToString());
        }
    }
}
