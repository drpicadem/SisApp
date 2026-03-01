using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Filters; // For UserException
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests; 
using ŠišAppApi.Models.SearchObjects; 

namespace ŠišAppApi.Services
{
    public class AppointmentService : BaseCRUDService<AppointmentDto, AppointmentSearchObject, Appointment, AppointmentInsertRequest, AppointmentUpdateRequest>, IAppointmentService
    {
        public AppointmentService(ApplicationDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        public override async Task<IEnumerable<AppointmentDto>> Get(AppointmentSearchObject search = null)
        {
            var query = _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Service)
                .Include(a => a.Barber).ThenInclude(b => b.User) // Include Barber User
                .Include(a => a.User)
                .AsQueryable();

            if (search != null)
            {
                // 1. RBAC Filters
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

                // 2. Specific Filters
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
                        // Active = future date AND not cancelled
                        query = query.Where(a => a.AppointmentDateTime >= DateTime.UtcNow && a.Status != "Cancelled");
                    }
                    else
                    {
                        // History = past date OR cancelled
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
            // Pagination
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

        // Custom Insert to handle logic
        public async Task<AppointmentDto> Insert(AppointmentInsertRequest request, int userId)
        {
             // 1. Validation Logic
             var appDateTime = request.AppointmentDateTime;
             // Fetch service duration or default to 30
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

             // 2. Map & Prepare
             var entity = _mapper.Map<Appointment>(request);
             entity.UserId = userId; // Set from context
             entity.Status = "Pending";
             entity.PaymentStatus = "Pending";
             entity.CreatedAt = DateTime.UtcNow;

             _context.Appointments.Add(entity);
             await _context.SaveChangesAsync();

             return _mapper.Map<AppointmentDto>(entity);
        }
        
        // Hide base Insert if we want to force usage of Insert with UserId, 
        // or ensure Controller calls the specific one. Base Insert usage might fail validation if UserID is required.
        public override async Task<AppointmentDto> Insert(AppointmentInsertRequest request)
        {
             throw new InvalidOperationException("Use Insert(request, userId) for Appointments");
        }

        public async Task<IEnumerable<string>> GetAvailableSlots(int barberId, DateOnly date, int? serviceId = null)
        {
            // 1. Get Service Duration
            int durationMinutes = 30; // Default
            if (serviceId.HasValue)
            {
               var service = await _context.Services.FindAsync(serviceId.Value);
               if (service != null && service.DurationMinutes > 0)
                   durationMinutes = service.DurationMinutes;
            }

            // 2. Get Working Hours
            var dayOfWeek = (int)date.DayOfWeek;
            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(w => w.BarberId == barberId && w.DayOfWeek == dayOfWeek && w.IsWorking);

            var availableSlots = new List<string>();
            TimeSpan currentTime;
            TimeSpan endTime;

            if (workingHours == null)
            {
                if (dayOfWeek == 0) return availableSlots; // Closed Sunday
                currentTime = new TimeSpan(9, 0, 0);
                endTime = new TimeSpan(17, 0, 0);
            }
            else
            {
                currentTime = workingHours.StartTime;
                endTime = workingHours.EndTime;
            }

            // 3. Get existing appointments
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

            // 4. Generate slots
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

        public async Task<AppointmentDto> Cancel(int id, int userId)
        {
            var entity = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null)
            {
                throw new UserException("Rezervacija nije pronađena.");
            }

            // Check if user owns the appointment (or is Admin/Barber - but for now strictly user own cancel)
            // If we want admins to use this method, we might need to relax this check or handle in Controller
            // But this specific method is 'Cancel' usually user initiated.
            if (entity.UserId != userId)
            {
                // Optionally allow logic for Barber/Admin if needed, but for "My Reservations" feature:
                throw new UserException("Nemate pravo otkazati ovu rezervaciju.");
            }

            if (entity.Status != "Pending")
            {
                throw new UserException("Moguće je otkazati samo rezervacije koje su na čekanju.");
            }

            // Logic to restrict time? e.g. 24h before
            // User requested "jednostavnu opciju otkazivanja", implied constraint-free for now unless specified.
            // But usually we don't allow cancelling past appointments.
            if (entity.AppointmentDateTime < DateTime.UtcNow)
            {
                 throw new UserException("Ne možete otkazati termin koji je već prošao.");
            }

            entity.Status = "Cancelled";
            entity.CancelledAt = DateTime.UtcNow;
            entity.CancellationReason = "Korisnik otkazao putem aplikacije";

            await _context.SaveChangesAsync();
            return _mapper.Map<AppointmentDto>(entity);
        }
    }
}