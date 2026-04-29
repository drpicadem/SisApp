using MassTransit;
using ŠišApp.Contracts;

namespace ŠišAppApi.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IPublishEndpoint publishEndpoint, ILogger<EmailService> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("Skipping email publish because recipient is empty.");
                    return;
                }

                var message = new SendEmailEvent
                {
                    ToEmail = toEmail,
                    Subject = subject,
                    Body = body
                };

                await _publishEndpoint.Publish(message);
                _logger.LogInformation("Email event published to RabbitMQ for {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing email event for {ToEmail}", toEmail);
            }
        }
    }
}
