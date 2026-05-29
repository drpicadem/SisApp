using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Helpers;
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

        public override Task<AppointmentDto> GetById(int id)
        {
            throw new UnauthorizedAccessException();
        }

        public async Task<AppointmentDto> GetByIdForUser(int id, int userId, string userRole)
        {
            var entity = await LoadAppointmentAsync(id);
            if (entity == null)
                throw new NotFoundException("Termin nije pronađen.");

            if (userRole != AppRoles.Admin)
            {
                if (userRole == AppRoles.User)
                {
                    if (entity.UserId != userId)
                        throw new UnauthorizedAccessException();
                }
                else if (userRole == AppRoles.Barber)
                {
                    if (entity.Barber == null || entity.Barber.UserId != userId)
                        throw new UnauthorizedAccessException();
                }
                else
                {
                    throw new UnauthorizedAccessException();
                }
            }

            return _mapper.Map<AppointmentDto>(entity);
        }

        private async Task<Appointment?> LoadAppointmentAsync(int id)
        {
            return await _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Service)
                .Include(a => a.Barber).ThenInclude(b => b.User)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        private static TimeZoneInfo GetSalonTimeZone() => SalonDateTimeHelper.GetSalonTimeZone();

        public async Task<AppointmentDto> Insert(AppointmentInsertRequest request, int userId)
        {
             var salonTz = GetSalonTimeZone();
             var appDateTimeUtc = SalonDateTimeHelper.NormalizeToUtc(request.AppointmentDateTime);
             var appDateTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(appDateTimeUtc, salonTz);

             if (appDateTimeUtc <= DateTime.UtcNow)
                 throw new UserException("Nije moguće rezervisati termin u prošlosti.");

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

             var newAppEndUtc = appDateTimeUtc.AddMinutes(duration);
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
                    a.AppointmentDateTime == appDateTimeUtc &&
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
                    a.AppointmentDateTime < newAppEndUtc &&
                    a.AppointmentDateTime.AddMinutes(
                        (a.Service != null && a.Service.DurationMinutes > 0) ? a.Service.DurationMinutes : 30
                    ) > appDateTimeUtc
                 );

             if (isTaken)
             {
                 throw new UserException("Termin je već zauzet.");
             }

             var entity = _mapper.Map<Appointment>(request);
             entity.UserId = userId;
             entity.AppointmentDateTime = appDateTimeUtc;
             entity.Status = AppointmentStatuses.Pending;
             entity.PaymentStatus = AppointmentPaymentStatuses.Pending;
             entity.CreatedAt = DateTime.UtcNow;

             _context.Appointments.Add(entity);
             await _context.SaveChangesAsync();

             var serviceName = service.Name ?? "usluga";
             var appointmentTime = SalonDateTimeHelper.FormatForDisplay(entity.AppointmentDateTime);

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

            var salonTz = GetSalonTimeZone();
            var dayOfWeek = (int)date.DayOfWeek;
            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(w => w.BarberId == barberId && w.DayOfWeek == dayOfWeek && w.IsWorking && !w.IsDeleted);

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

            var localDayStart = date.ToDateTime(TimeOnly.MinValue);
            var localDayEnd = date.ToDateTime(new TimeOnly(23, 59, 59, 999));
            var startUtc = SalonDateTimeHelper.SalonLocalToUtc(localDayStart);
            var endUtc = SalonDateTimeHelper.SalonLocalToUtc(localDayEnd);

            var existingAppointments = await _context.Appointments
                .Include(a => a.Service)
                .Where(a =>
                            a.BarberId == barberId &&
                            a.AppointmentDateTime >= startUtc &&
                            a.AppointmentDateTime <= endUtc &&
                            a.Status != AppointmentStatuses.Cancelled)
                .ToListAsync();

            var nowSalonLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, salonTz);
            var isToday = date == DateOnly.FromDateTime(nowSalonLocal);
            var currentTimeNow = TimeOnly.FromDateTime(nowSalonLocal);

            while (currentTime.Add(TimeSpan.FromMinutes(durationMinutes)) <= endTime)
            {
                var slotLocalStart = date.ToDateTime(TimeOnly.FromTimeSpan(currentTime));
                var slotStartUtc = SalonDateTimeHelper.SalonLocalToUtc(slotLocalStart);
                var slotEndUtc = slotStartUtc.AddMinutes(durationMinutes);

                if (isToday && TimeOnly.FromTimeSpan(currentTime) <= currentTimeNow)
                {
                     currentTime = currentTime.Add(TimeSpan.FromMinutes(durationMinutes));
                     continue;
                }

                bool isOverlap = existingAppointments.Any(a =>
                {
                    var appStartUtc = SalonDateTimeHelper.NormalizeToUtc(a.AppointmentDateTime);
                    var existingDuration = (a.Service != null && a.Service.DurationMinutes > 0) ? a.Service.DurationMinutes : 30;
                    var appEndUtc = appStartUtc.AddMinutes(existingDuration);
                    return slotStartUtc < appEndUtc && slotEndUtc > appStartUtc;
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

            if (entity.PaymentStatus == AppointmentPaymentStatuses.Paid && userRole != AppRoles.Admin)
            {
                throw new UserException(
                    "Ovaj termin je već plaćen i ne može biti otkazan putem aplikacije. " +
                    "Molimo kontaktirajte administratora za povrat sredstava.");
            }

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
            entity.CancelledByUserId = userId;

            if (entity.PaymentStatus == AppointmentPaymentStatuses.Paid && userRole == AppRoles.Admin)
                entity.PaymentStatus = AppointmentPaymentStatuses.RefundRequired;

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