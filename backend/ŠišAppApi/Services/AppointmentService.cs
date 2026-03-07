using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests; 
using ŠišAppApi.Models.SearchObjects; 

namespace ŠišAppApi.Services
{
    public class AppointmentService : BaseCRUDService<AppointmentDto, AppointmentSearchObject, Appointment, AppointmentInsertRequest, AppointmentUpdateRequest>, IAppointmentService
    {
        private readonly INotificationService _notificationService;

        public AppointmentService(ApplicationDbContext context, IMapper mapper, INotificationService notificationService) : base(context, mapper)
        {
            _notificationService = notificationService;
        }

        public override async Task<IEnumerable<AppointmentDto>> Get(AppointmentSearchObject search = null)
        {
            var query = _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Service)
                .Include(a => a.Barber).ThenInclude(b => b.User)
                .Include(a => a.User)
                .AsQueryable();

            if (search != null)
            {
                if (search.CurrentUserRole == "User" && search.CurrentUserId.HasValue)
                {
                    query = query.Where(a => a.UserId == search.CurrentUserId);
                }
                else if (search.CurrentUserRole == "Barber" && search.CurrentUserId.HasValue)
                {
                     query = query.Where(a => a.Barber.UserId == search.CurrentUserId);
                }
                else if (search.CurrentUserRole == "Admin" || search.CurrentUserRole == "SuperAdmin") 
                {
                    
                }

                if (search.UserId.HasValue)
                {
                    query = query.Where(a => a.UserId == search.UserId);
                }
                
                if (search.BarberId.HasValue)
                {
                    query = query.Where(a => a.BarberId == search.BarberId);
                }

                if (search.FromDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDateTime >= search.FromDate.Value);
                }

                if (search.ToDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDateTime <= search.ToDate.Value);
                }

                if (!string.IsNullOrEmpty(search.Status))
                {
                    query = query.Where(a => a.Status == search.Status);
                }

                if (search.IsActive.HasValue)
                {
                    if (search.IsActive.Value)
                    {
                        query = query.Where(a => a.AppointmentDateTime >= DateTime.UtcNow && a.Status != "Cancelled");
                    }
                    else
                    {
                        query = query.Where(a => a.AppointmentDateTime < DateTime.UtcNow || a.Status == "Cancelled");
                    }
                }

                if (search.IsPaid.HasValue)
                {
                   if (search.IsPaid.Value)
                   {
                        query = query.Where(a => a.PaymentStatus == "Paid");
                   }
                   else
                   {
                        query = query.Where(a => a.PaymentStatus != "Paid" || a.PaymentStatus == null);
                   }
                }
            }
            if (search?.Page.HasValue == true && search?.PageSize.HasValue == true)
            {
                query = query.Skip((search.Page.Value - 1) * search.PageSize.Value).Take(search.PageSize.Value);
            }

            var list = await query.OrderByDescending(a => a.AppointmentDateTime).ToListAsync();
            return _mapper.Map<List<AppointmentDto>>(list);
        }

        public override async Task<AppointmentDto> GetById(int id)
        {
             var entity = await _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Service)
                .Include(a => a.Barber).ThenInclude(b => b.User)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

             return _mapper.Map<AppointmentDto>(entity);
        }

        public async Task<AppointmentDto> Insert(AppointmentInsertRequest request, int userId)
        {
             var appDateTime = request.AppointmentDateTime;
             int duration = 30;
             if (request.ServiceId > 0)
             {
                 var service = await _context.Services.FindAsync(request.ServiceId);
                 if (service != null && service.DurationMinutes > 0)
                     duration = service.DurationMinutes;
             }
             
             var newAppEnd = appDateTime.AddMinutes(duration);

             var isTaken = await _context.Appointments
                 .Include(a => a.Service)
                 .AnyAsync(a => 
                    a.BarberId == request.BarberId && 
                    a.Status != "Cancelled" &&
                    a.AppointmentDateTime < newAppEnd && 
                    a.AppointmentDateTime.AddMinutes(
                        (a.Service != null && a.Service.DurationMinutes > 0) ? a.Service.DurationMinutes : 30
                    ) > appDateTime 
                 );

             if (isTaken)
             {
                 throw new UserException("Termin je već zauzet.");
             }

             var entity = _mapper.Map<Appointment>(request);
             entity.UserId = userId;
             entity.Status = "Pending";
             entity.PaymentStatus = "Pending";
             entity.CreatedAt = DateTime.UtcNow;

             _context.Appointments.Add(entity);
             await _context.SaveChangesAsync();

             return _mapper.Map<AppointmentDto>(entity);
        }
        
        public override async Task<AppointmentDto> Insert(AppointmentInsertRequest request)
        {
             throw new InvalidOperationException("Use Insert(request, userId) for Appointments");
        }

        public async Task<IEnumerable<string>> GetAvailableSlots(int barberId, DateOnly date, int? serviceId = null)
        {
            int durationMinutes = 30;
            if (serviceId.HasValue)
            {
               var service = await _context.Services.FindAsync(serviceId.Value);
               if (service != null && service.DurationMinutes > 0)
                   durationMinutes = service.DurationMinutes;
            }

            var dayOfWeek = (int)date.DayOfWeek;
            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(w => w.BarberId == barberId && w.DayOfWeek == dayOfWeek && w.IsWorking);

            var availableSlots = new List<string>();
            TimeSpan currentTime;
            TimeSpan endTime;

            if (workingHours == null)
            {
                if (dayOfWeek == 0) return availableSlots;
                currentTime = new TimeSpan(9, 0, 0);
                endTime = new TimeSpan(17, 0, 0);
            }
            else
            {
                currentTime = workingHours.StartTime;
                endTime = workingHours.EndTime;
            }

            var startDateTime = date.ToDateTime(TimeOnly.MinValue);
            var endDateTime = date.ToDateTime(TimeOnly.MaxValue);
            
            var existingAppointments = await _context.Appointments
                .Include(a => a.Service)
                .Where(a =>
                            a.BarberId == barberId &&
                            a.AppointmentDateTime >= startDateTime && 
                            a.AppointmentDateTime <= endDateTime &&
                            a.Status != "Cancelled")
                .ToListAsync();

            TimeZoneInfo tz;
            try 
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo");
            } 
            catch 
            {   
                try 
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                } 
                catch 
                {
                    tz = TimeZoneInfo.Local;
                }
            }
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var isToday = date == DateOnly.FromDateTime(now);
            var currentTimeNow = TimeOnly.FromDateTime(now);

            while (currentTime.Add(TimeSpan.FromMinutes(durationMinutes)) <= endTime)
            {
                var slotStart = date.ToDateTime(TimeOnly.FromTimeSpan(currentTime));
                var slotEnd = slotStart.AddMinutes(durationMinutes);

                if (isToday && TimeOnly.FromTimeSpan(currentTime) <= currentTimeNow) 
                {
                     currentTime = currentTime.Add(TimeSpan.FromMinutes(durationMinutes));
                     continue;
                }

                bool isOverlap = existingAppointments.Any(a =>
                {
                    var appStart = a.AppointmentDateTime;
                    var existingDuration = (a.Service != null && a.Service.DurationMinutes > 0) ? a.Service.DurationMinutes : 30;
                    var appEnd = appStart.AddMinutes(existingDuration);
                    return (slotStart < appEnd && slotEnd > appStart);
                });

                if (!isOverlap)
                {
                    availableSlots.Add(currentTime.ToString(@"hh\:mm"));
                }

                currentTime = currentTime.Add(TimeSpan.FromMinutes(durationMinutes)); 
            }

            return availableSlots;
        }

        public async Task<AppointmentDto> Cancel(int id, int userId, string userRole)
        {
            var entity = await _context.Appointments
                .Include(a => a.Barber)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null)
                throw new UserException("Rezervacija nije pronađena.");

            if (entity.Status == "Cancelled")
                throw new UserException("Termin je već otkazan.");

            if (entity.Status == "Completed")
                throw new UserException("Nije moguće otkazati završen termin.");

            if (entity.Status != "Pending" && entity.Status != "Confirmed")
                throw new UserException("Nije moguće otkazati termin sa statusom: " + entity.Status);

            if (entity.AppointmentDateTime < DateTime.UtcNow)
                throw new UserException("Ne možete otkazati termin koji je već prošao.");

            string cancellationReason;

            if (userRole == "Admin")
            {
                cancellationReason = "Admin otkazao putem aplikacije";
            }
            else if (userRole == "Barber")
            {
                if (entity.Barber == null || entity.Barber.UserId != userId)
                    throw new UserException("Nemate pravo otkazati ovaj termin.");
                cancellationReason = "Frizer otkazao putem aplikacije";
            }
            else
            {
                if (entity.UserId != userId)
                    throw new UserException("Nemate pravo otkazati ovaj termin.");
                cancellationReason = "Korisnik otkazao putem aplikacije";
            }

            entity.Status = "Cancelled";
            entity.CancelledAt = DateTime.UtcNow;
            entity.CancellationReason = cancellationReason;

            await _context.SaveChangesAsync();

            if (userRole == "Barber" || userRole == "Admin")
            {
                var serviceName = entity.Service?.Name ?? "usluga";
                var message = $"Vaš termin za '{serviceName}' je otkazan od strane {(userRole == "Barber" ? "frizera" : "administratora")}.";
                await _notificationService.CreateNotification(entity.UserId, message, "Cancellation");
            }

            return _mapper.Map<AppointmentDto>(entity);
        }
    }
}