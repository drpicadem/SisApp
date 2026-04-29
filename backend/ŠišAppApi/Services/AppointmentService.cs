using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Constants;
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
        private const int SundayDayOfWeek = (int)DayOfWeek.Sunday;
        private const int UserCancellationHoursLimit = 25;
        private const int StaffCancellationHoursLimit = 48;
        private const string PaymentCancelReasonTag = "AUTOCANCEL_PAYMENT_NOT_COMPLETED";
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
            var page = Math.Max(1, search?.Page ?? 1);
            var pageSize = Math.Clamp(search?.PageSize ?? 20, 1, 100);

            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Q))
                {
                    var q = search.Q.Trim().ToLower();
                    query = query.Where(a =>
                        (a.Status != null && a.Status.ToLower().Contains(q)) ||
                        (a.Salon != null && a.Salon.Name.ToLower().Contains(q)) ||
                        (a.Service != null && a.Service.Name.ToLower().Contains(q)) ||
                        (a.Barber != null && a.Barber.User != null &&
                         ((a.Barber.User.FirstName + " " + a.Barber.User.LastName).ToLower().Contains(q) ||
                          a.Barber.User.Username.ToLower().Contains(q))));
                }
                if (search.CurrentUserRole == AppRoles.User && search.CurrentUserId.HasValue)
                {
                    query = query.Where(a => a.UserId == search.CurrentUserId);
                }
                else if (search.CurrentUserRole == AppRoles.Barber && search.CurrentUserId.HasValue)
                {
                     query = query.Where(a => a.Barber.UserId == search.CurrentUserId);
                }
                else if (search.CurrentUserRole == AppRoles.Admin)
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
                        query = query.Where(a => a.AppointmentDateTime >= DateTime.UtcNow && a.Status != AppointmentStatuses.Cancelled);
                    }
                    else
                    {
                        query = query.Where(a => a.AppointmentDateTime < DateTime.UtcNow || a.Status == AppointmentStatuses.Cancelled);
                    }
                }

                if (search.IsPaid.HasValue)
                {
                   if (search.IsPaid.Value)
                   {
                        query = query.Where(a => a.PaymentStatus == AppointmentPaymentStatuses.Paid);
                   }
                   else
                   {
                        query = query.Where(a => a.PaymentStatus != AppointmentPaymentStatuses.Paid || a.PaymentStatus == null);
                   }
                }
            }

            bool isUpcoming = search?.IsActive == true;
            var list = isUpcoming
                ? await query.OrderBy(a => a.AppointmentDateTime).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
                : await query.OrderByDescending(a => a.AppointmentDateTime).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
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

        private static TimeZoneInfo GetSalonTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
                catch { return TimeZoneInfo.Local; }
            }
        }

        public async Task<AppointmentDto> Insert(AppointmentInsertRequest request, int userId)
        {
             var appDateTime = request.AppointmentDateTime;
             var salonTz = GetSalonTimeZone();
             var appDateTimeLocal = appDateTime.Kind == DateTimeKind.Utc
                 ? TimeZoneInfo.ConvertTimeFromUtc(appDateTime, salonTz)
                 : appDateTime;
             int duration = 30;
             var salonExists = await _context.Salons
                 .AnyAsync(s => s.Id == request.SalonId && !s.IsDeleted && s.IsActive);
             if (!salonExists)
                 throw new UserException("Odabrani salon nije dostupan.");

             var service = await _context.Services
                 .AsNoTracking()
                 .FirstOrDefaultAsync(s => s.Id == request.ServiceId && !s.IsDeleted && s.IsActive);
             if (service == null)
                 throw new UserException("Odabrana usluga nije dostupna.");

             if (service.SalonId != request.SalonId)
                 throw new UserException("Odabrana usluga ne pripada izabranom salonu.");

             if (service.DurationMinutes > 0)
                 duration = service.DurationMinutes;

             var barber = await _context.Barbers
                 .AsNoTracking()
                 .FirstOrDefaultAsync(b => b.Id == request.BarberId && !b.IsDeleted && b.IsAvailable);
             if (barber == null)
                 throw new UserException("Odabrani frizer nije dostupan.");

             if (barber.SalonId != request.SalonId)
                 throw new UserException("Odabrani frizer ne pripada izabranom salonu.");

             var barberSupportsService = await _context.BarberSpecialties
                 .AnyAsync(bs =>
                    bs.BarberId == request.BarberId &&
                    bs.ServiceId == request.ServiceId &&
                    !bs.IsDeleted);
             if (!barberSupportsService)
                 throw new UserException("Odabrani frizer ne pruža odabranu uslugu.");

             var newAppEnd = appDateTime.AddMinutes(duration);
             var dayOfWeek = (int)appDateTimeLocal.DayOfWeek;
             var workingHours = await _context.WorkingHours
                 .FirstOrDefaultAsync(w =>
                     w.BarberId == request.BarberId &&
                     w.DayOfWeek == dayOfWeek &&
                     !w.IsDeleted);

             TimeSpan workStart;
             TimeSpan workEnd;
             if (workingHours == null)
             {
                if (dayOfWeek == SundayDayOfWeek)
                 {
                     throw new UserException("Frizer ne radi odabrani dan.");
                 }

                 workStart = new TimeSpan(9, 0, 0);
                 workEnd = new TimeSpan(17, 0, 0);
             }
             else
             {
                 if (!workingHours.IsWorking)
                 {
                     throw new UserException("Frizer ne radi odabrani dan.");
                 }

                 workStart = workingHours.StartTime;
                 workEnd = workingHours.EndTime;
             }

             var appointmentStart = appDateTimeLocal.TimeOfDay;
             var appointmentEnd = appointmentStart.Add(TimeSpan.FromMinutes(duration));
             if (appointmentStart < workStart || appointmentEnd > workEnd)
             {
                 throw new UserException("Termin je van radnog vremena frizera.");
             }

             var hasDuplicateForUser = await _context.Appointments
                 .AnyAsync(a =>
                    a.UserId == userId &&
                    a.ServiceId == request.ServiceId &&
                    a.AppointmentDateTime == appDateTime &&
                    a.Status != AppointmentStatuses.Cancelled
                 );

             if (hasDuplicateForUser)
             {
                 throw new UserException("Već imate rezervaciju za isti termin i uslugu.");
             }

             var isTaken = await _context.Appointments
                 .Include(a => a.Service)
                 .AnyAsync(a =>
                    a.BarberId == request.BarberId &&
                    a.Status != AppointmentStatuses.Cancelled &&
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
             entity.Status = AppointmentStatuses.Pending;
             entity.PaymentStatus = AppointmentPaymentStatuses.Pending;
             entity.CreatedAt = DateTime.UtcNow;

             _context.Appointments.Add(entity);
             await _context.SaveChangesAsync();

             var serviceName = service.Name ?? "usluga";
             var appointmentTime = entity.AppointmentDateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

             await _notificationService.CreateNotification(
                 entity.UserId,
                 $"Rezervacija za '{serviceName}' je uspješno kreirana za {appointmentTime}.",
                 NotificationTypes.Appointment,
                 entity.Id.ToString(),
                 "Rezervacija kreirana");

             if (barber.UserId > 0)
             {
                 await _notificationService.CreateNotification(
                     barber.UserId,
                     $"Nova rezervacija za '{serviceName}' zakazana je za {appointmentTime}.",
                     NotificationTypes.Appointment,
                     entity.Id.ToString(),
                     "Nova rezervacija");
             }

             return _mapper.Map<AppointmentDto>(entity);
        }

        public override Task<AppointmentDto> Insert(AppointmentInsertRequest request)
            => throw new InvalidOperationException("Use Insert(request, userId) for Appointments");

        public override async Task<AppointmentDto> Update(int id, AppointmentUpdateRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Status) || !string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                throw new UserException("Direktna izmjena statusa nije dozvoljena kroz ovaj endpoint.");
            }

            var entity = await _context.Appointments.FindAsync(id);
            if (entity == null)
            {
                throw new UserException("Rezervacija nije pronađena.");
            }

            entity.Notes = string.IsNullOrWhiteSpace(request.Note) ? entity.Notes : request.Note.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return _mapper.Map<AppointmentDto>(entity);
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
                if (dayOfWeek == SundayDayOfWeek) return availableSlots;
                currentTime = new TimeSpan(9, 0, 0);
                endTime = new TimeSpan(17, 0, 0);
            }
            else
            {
                currentTime = workingHours.StartTime;
                endTime = workingHours.EndTime;
            }

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

            var localDayStart = date.ToDateTime(TimeOnly.MinValue);
            var localDayEnd = date.ToDateTime(TimeOnly.MaxValue);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDayStart, DateTimeKind.Unspecified), tz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDayEnd, DateTimeKind.Unspecified), tz);

            var existingAppointments = await _context.Appointments
                .Include(a => a.Service)
                .Where(a =>
                            a.BarberId == barberId &&
                            a.AppointmentDateTime >= startUtc &&
                            a.AppointmentDateTime <= endUtc &&
                            a.Status != AppointmentStatuses.Cancelled)
                .ToListAsync();

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
                    // Appointments stored as UTC – convert to local for comparison with local slotStart
                    var appStartUtc = DateTime.SpecifyKind(a.AppointmentDateTime, DateTimeKind.Utc);
                    var appStart = TimeZoneInfo.ConvertTimeFromUtc(appStartUtc, tz);
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

        public async Task<AppointmentDto> Cancel(int id, int userId, string userRole, string? reason = null)
        {
            var entity = await _context.Appointments
                .Include(a => a.Barber)
                .ThenInclude(b => b.User)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null)
                throw new UserException("Rezervacija nije pronađena.");

            if (entity.Status == AppointmentStatuses.Cancelled)
                throw new UserException("Termin je već otkazan.");

            if (entity.Status == AppointmentStatuses.Completed)
                throw new UserException("Nije moguće otkazati završen termin.");

            if (!AppointmentStateMachine.CanCancel(entity.Status))
                throw new UserException("Nije moguće otkazati termin sa statusom: " + entity.Status);

            if (entity.AppointmentDateTime < DateTime.UtcNow)
                throw new UserException("Ne možete otkazati termin koji je već prošao.");

            var hoursUntilAppointment = (entity.AppointmentDateTime - DateTime.UtcNow).TotalHours;
            var isAutoPaymentCancel = userRole == AppRoles.User
                && string.Equals(reason?.Trim(), PaymentCancelReasonTag, StringComparison.Ordinal)
                && entity.Status == AppointmentStatuses.Pending
                && entity.PaymentStatus == AppointmentPaymentStatuses.Pending;

            if (userRole == AppRoles.User && !isAutoPaymentCancel && hoursUntilAppointment < UserCancellationHoursLimit)
            {
                throw new UserException($"Korisnik može otkazati termin najkasnije {UserCancellationHoursLimit}h prije početka.");
            }

            if ((userRole == AppRoles.Admin || userRole == AppRoles.Barber) && hoursUntilAppointment < StaffCancellationHoursLimit)
            {
                throw new UserException($"Administrator/frizer može otkazati termin najkasnije {StaffCancellationHoursLimit}h prije početka.");
            }

            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedReason) && normalizedReason.Length > 255)
                throw new UserException("Razlog otkazivanja može imati maksimalno 255 karaktera.");

            if (userRole == AppRoles.Admin)
            {
                normalizedReason ??= "Admin otkazao putem aplikacije";
            }
            else if (userRole == AppRoles.Barber)
            {
                if (entity.Barber == null || entity.Barber.UserId != userId)
                    throw new UserException("Nemate pravo otkazati ovaj termin.");
                normalizedReason ??= "Frizer otkazao putem aplikacije";
            }
            else
            {
                if (entity.UserId != userId)
                    throw new UserException("Nemate pravo otkazati ovaj termin.");
                normalizedReason ??= isAutoPaymentCancel
                    ? "Automatski otkazano: plaćanje nije završeno"
                    : "Korisnik otkazao putem aplikacije";
            }

            AppointmentStateMachine.EnsureTransition(entity.Status, AppointmentStatuses.Cancelled);
            entity.Status = AppointmentStatuses.Cancelled;
            entity.CancelledAt = DateTime.UtcNow;
            entity.CancellationReason = normalizedReason;

            await _context.SaveChangesAsync();

            if (userRole == AppRoles.Barber || userRole == AppRoles.Admin)
            {
                var serviceName = entity.Service?.Name ?? "usluga";
                var message = $"Vaš termin za '{serviceName}' je otkazan od strane {(userRole == AppRoles.Barber ? "frizera" : "administratora")}.";
                await _notificationService.CreateNotification(entity.UserId, message, NotificationTypes.Cancellation);
            }

            if (entity.Barber != null && entity.Barber.UserId > 0 && userRole != AppRoles.Barber)
            {
                var serviceName = entity.Service?.Name ?? "usluga";
                var actor = userRole == AppRoles.Admin ? "administrator" : "korisnik";
                var message = $"Termin za '{serviceName}' je otkazao {actor}.";
                await _notificationService.CreateNotification(entity.Barber.UserId, message, NotificationTypes.Cancellation);
            }

            return _mapper.Map<AppointmentDto>(entity);
        }
    }
}