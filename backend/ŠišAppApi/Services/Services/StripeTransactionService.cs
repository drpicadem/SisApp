using Microsoft.EntityFrameworkCore;
using Stripe;
using ŠišAppApi.Constants;
using ŠišAppApi.Data;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Services.Services
{
    public class StripeTransactionService : IStripeTransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<StripeTransactionService> _logger;

        public StripeTransactionService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<StripeTransactionService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AppointmentForPaymentDto?> GetAppointmentForPaymentAsync(int appointmentId, int userId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return null;

            if (appointment.UserId != userId)
                return new AppointmentForPaymentDto { Id = -1 };

            var alreadyPaid = appointment.PaymentStatus == AppointmentPaymentStatuses.Paid;
            var amountInCents = (long)Math.Round((appointment.Service?.Price ?? 0m) * 100m, MidpointRounding.AwayFromZero);

            return new AppointmentForPaymentDto
            {
                Id = appointment.Id,
                UserId = appointment.UserId,
                AlreadyPaid = alreadyPaid,
                AmountInCents = amountInCents,
                ExistingPaymentIntentId = appointment.PaymentIntentId
            };
        }

        public async Task<StripePaymentIntentData> CreatePaymentIntentAsync(int appointmentId, long amountInCents)
        {
            var intentService = new PaymentIntentService();
            var paymentIntent = await intentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "eur",
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "appointmentId", appointmentId.ToString() }
                }
            });

            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                appointment.PaymentIntentId = paymentIntent.Id;
                await _context.SaveChangesAsync();
            }

            return new StripePaymentIntentData
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                AmountInCents = amountInCents
            };
        }

        public async Task<StripeCompletePurchaseResult> CompletePurchaseAsync(int appointmentId, int userId, string? paymentIntentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                throw new NotFoundException("Termin nije pronađen.");

            if (appointment.UserId != userId)
                throw new UnauthorizedAccessException();

            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);
            if (appointment.PaymentStatus == AppointmentPaymentStatuses.Paid && existingPayment != null)
                return new StripeCompletePurchaseResult { AlreadyCompleted = true };

            var intentId = string.IsNullOrWhiteSpace(paymentIntentId) ? appointment.PaymentIntentId : paymentIntentId;
            if (string.IsNullOrWhiteSpace(intentId))
                throw new UserException("Nedostaje PaymentIntentId.");

            var intentService = new PaymentIntentService();
            var intent = await intentService.GetAsync(intentId);

            if (intent.Status != "succeeded")
                return new StripeCompletePurchaseResult { PaymentNotSucceeded = true, StripeStatus = intent.Status };

            var expectedAmountInCents = (long)Math.Round((appointment.Service?.Price ?? 0m) * 100m, MidpointRounding.AwayFromZero);
            if (expectedAmountInCents <= 0)
                throw new UserException("Nevažeća cijena termina.");

            if (intent.Amount != expectedAmountInCents)
            {
                _logger.LogWarning("Payment amount mismatch for Appointment {AppointmentId}.", appointment.Id);
                return new StripeCompletePurchaseResult { AmountMismatch = true };
            }

            appointment.PaymentStatus = AppointmentPaymentStatuses.Paid;
            appointment.PaymentIntentId = intent.Id;

            if (existingPayment == null)
            {
                _context.Payments.Add(new Payment
                {
                    AppointmentId = appointmentId,
                    UserId = appointment.UserId,
                    Amount = intent.Amount / 100m,
                    Currency = intent.Currency,
                    Method = PaymentMethods.Stripe,
                    TransactionId = intent.Id,
                    Status = PaymentStatuses.Completed,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await SendNotificationsAsync(appointment);

            return new StripeCompletePurchaseResult();
        }

        private async Task SendNotificationsAsync(Models.Appointment appointment)
        {
            try
            {
                var paymentRecord = await _context.Payments.FirstOrDefaultAsync(p => p.AppointmentId == appointment.Id);
                await _notificationService.CreateNotification(
                    appointment.UserId,
                    $"Uspješno ste platili rezervaciju termina za {appointment.AppointmentDateTime.ToLocalTime():dd.MM.yyyy HH:mm}.",
                    NotificationTypes.Payment,
                    paymentRecord?.Id.ToString());

                var user = await _context.Users.FindAsync(appointment.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Potvrda rezervacije - ŠišApp",
                        $"<h1>Rezervacija potvrđena!</h1><p>Poštovani/a {user.FirstName},</p><p>Vaša rezervacija za termin <strong>{appointment.AppointmentDateTime.ToLocalTime():dd.MM.yyyy HH:mm}</strong> je uspješno plaćena.</p><p>Hvala na povjerenju!</p>");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notification/email after payment failed");
            }
        }
    }
}
