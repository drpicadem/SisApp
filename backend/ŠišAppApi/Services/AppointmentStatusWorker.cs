using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;

namespace ŠišAppApi.Services;

public class AppointmentStatusWorker : BackgroundService
{
    private static readonly string[] ReminderEligibleStatuses = [AppointmentStatuses.Pending, AppointmentStatuses.Confirmed];
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentStatusWorker> _logger;

    public AppointmentStatusWorker(IServiceScopeFactory scopeFactory, ILogger<AppointmentStatusWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAppointments(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing appointment statuses.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ProcessAppointments(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;

        await SendOneHourReminders(context, notificationService, now, cancellationToken);
        await UpdateStatuses(context, notificationService, now, cancellationToken);
    }

    private static async Task SendOneHourReminders(
        ApplicationDbContext context,
        INotificationService notificationService,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var reminderWindowStart = now.AddMinutes(59);
        var reminderWindowEnd = now.AddMinutes(60);

        var appointments = await context.Appointments
            .Include(a => a.Service)
            .Where(a =>
                !a.IsDeleted &&
                ReminderEligibleStatuses.Contains(a.Status) &&
                a.AppointmentDateTime >= reminderWindowStart &&
                a.AppointmentDateTime < reminderWindowEnd)
            .ToListAsync(cancellationToken);

        var reminderDataByAppointmentId = appointments.ToDictionary(
            a => a.Id,
            a => $"appointment-reminder-1h:{a.Id}");

        var userIds = appointments
            .Select(a => a.UserId)
            .Distinct()
            .ToList();

        var reminderDataValues = reminderDataByAppointmentId.Values.ToList();

        var existingReminderData = await context.Notifications
            .Where(n =>
                !n.IsDeleted &&
                n.Type == NotificationTypes.AppointmentReminder &&
                userIds.Contains(n.UserId) &&
                n.Data != null &&
                reminderDataValues.Contains(n.Data))
            .Select(n => n.Data!)
            .ToListAsync(cancellationToken);

        var existingReminderDataSet = existingReminderData.ToHashSet();

        foreach (var appointment in appointments)
        {
            var reminderData = reminderDataByAppointmentId[appointment.Id];

            if (existingReminderDataSet.Contains(reminderData))
            {
                continue;
            }

            var serviceName = appointment.Service?.Name ?? "uslugu";
            var message = $"Podsjetnik: Vaš termin za {serviceName} počinje za 1 sat.";

            await notificationService.CreateNotification(
                appointment.UserId,
                message,
                NotificationTypes.AppointmentReminder,
                reminderData,
                "Podsjetnik za termin");

            existingReminderDataSet.Add(reminderData);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpdateStatuses(
        ApplicationDbContext context,
        INotificationService notificationService,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var appointments = await context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Where(a =>
                !a.IsDeleted &&
                a.Status != AppointmentStatuses.Cancelled &&
                a.Status != AppointmentStatuses.Completed)
            .ToListAsync(cancellationToken);

        var hasChanges = false;

        foreach (var appointment in appointments)
        {
            var durationMinutes = appointment.Service?.DurationMinutes > 0 ? appointment.Service.DurationMinutes : 30;
            var endTime = appointment.AppointmentDateTime.AddMinutes(durationMinutes);

            if (now >= endTime)
            {
                if (appointment.Status != AppointmentStatuses.Completed &&
                    AppointmentStateMachine.CanTransition(appointment.Status, AppointmentStatuses.Completed))
                {
                    appointment.Status = AppointmentStatuses.Completed;
                    appointment.UpdatedAt = now;
                    hasChanges = true;

                    var serviceName = appointment.Service?.Name ?? "usluga";
                    await notificationService.CreateNotification(
                        appointment.UserId,
                        $"Vaš termin za '{serviceName}' je završen.",
                        NotificationTypes.Appointment,
                        appointment.Id.ToString(),
                        "Status termina ažuriran");

                    if (appointment.Barber != null && appointment.Barber.UserId > 0)
                    {
                        await notificationService.CreateNotification(
                            appointment.Barber.UserId,
                            $"Termin za '{serviceName}' je označen kao završen.",
                            NotificationTypes.Appointment,
                            appointment.Id.ToString(),
                            "Status termina ažuriran");
                    }
                }

                continue;
            }

            if (now >= appointment.AppointmentDateTime &&
                appointment.Status != AppointmentStatuses.Active &&
                AppointmentStateMachine.CanTransition(appointment.Status, AppointmentStatuses.Active))
            {
                appointment.Status = AppointmentStatuses.Active;
                appointment.UpdatedAt = now;
                hasChanges = true;

                var serviceName = appointment.Service?.Name ?? "usluga";
                await notificationService.CreateNotification(
                    appointment.UserId,
                    $"Vaš termin za '{serviceName}' je upravo počeo.",
                    NotificationTypes.Appointment,
                    appointment.Id.ToString(),
                    "Status termina ažuriran");

                if (appointment.Barber != null && appointment.Barber.UserId > 0)
                {
                    await notificationService.CreateNotification(
                        appointment.Barber.UserId,
                        $"Termin za '{serviceName}' je prešao u aktivan status.",
                        NotificationTypes.Appointment,
                        appointment.Id.ToString(),
                        "Status termina ažuriran");
                }
            }
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
