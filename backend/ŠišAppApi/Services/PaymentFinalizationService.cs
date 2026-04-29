using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Models;

namespace ŠišAppApi.Services;

public class PaymentFinalizationService : IPaymentFinalizationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public PaymentFinalizationService(
        ApplicationDbContext context,
        INotificationService notificationService,
        IEmailService emailService)
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<PaymentFinalizationResult> FinalizeAsync(PaymentFinalizationInput input)
    {
        var appointment = await _context.Appointments.FindAsync(input.AppointmentId);
        if (appointment == null)
        {
            return new PaymentFinalizationResult { AppointmentNotFound = true };
        }

        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p => p.AppointmentId == input.AppointmentId);

        if (existingPayment?.Status == PaymentStatuses.Completed)
        {
            return new PaymentFinalizationResult
            {
                AlreadyPaid = true,
                PaymentId = existingPayment?.Id
            };
        }

        appointment.PaymentStatus = AppointmentPaymentStatuses.Paid;
        Payment payment;
        if (existingPayment != null)
        {
            existingPayment.Amount = input.Amount;
            existingPayment.Currency = (input.Currency ?? "EUR").ToUpperInvariant();
            existingPayment.Method = input.Method;
            existingPayment.TransactionId = input.TransactionId;
            existingPayment.Status = PaymentStatuses.Completed;
            existingPayment.UpdatedAt = DateTime.UtcNow;
            payment = existingPayment;
        }
        else
        {
            payment = new Payment
            {
                AppointmentId = input.AppointmentId,
                UserId = appointment.UserId,
                Amount = input.Amount,
                Currency = (input.Currency ?? "EUR").ToUpperInvariant(),
                Method = input.Method,
                TransactionId = input.TransactionId,
                Status = PaymentStatuses.Completed,
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
        }

        await _context.SaveChangesAsync();

        try
        {
            await _notificationService.CreateNotification(
                appointment.UserId,
                $"Uspješno ste platili rezervaciju termina za {appointment.AppointmentDateTime.ToLocalTime():dd.MM.yyyy HH:mm}.",
                NotificationTypes.Payment,
                payment.Id.ToString());

            var user = await _context.Users.FindAsync(appointment.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Potvrda rezervacije - ŠišApp",
                    $"<h1>Rezervacija potvrđena!</h1><p>Poštovani/a {user.FirstName},</p><p>Vaša rezervacija za termin <strong>{appointment.AppointmentDateTime.ToLocalTime():dd.MM.yyyy HH:mm}</strong> je uspješno plaćena.</p><p>Hvala na povjerenju!</p>");
            }
        }
        catch (Exception)
        {
        }

        return new PaymentFinalizationResult
        {
            PaymentId = payment.Id
        };
    }
}
