using MassTransit;
using System.Net;
using System.Net.Mail;
using ŠišApp.Contracts;

namespace ŠišAppApi.Services.Consumers
{
    public class EmailConsumer : IConsumer<SendEmailEvent>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailConsumer> _logger;

        public EmailConsumer(IConfiguration configuration, ILogger<EmailConsumer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SendEmailEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation($"Sending email to {message.ToEmail} with subject: {message.Subject}");

            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail))
                {
                    _logger.LogWarning("Email settings are not configured properly. Skipping email sending.");
                    return;
                }

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "ŠišApp"),
                    Subject = message.Subject,
                    Body = message.Body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(message.ToEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {message.ToEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {message.ToEmail}");
                // We might want to throw here to let MassTransit retry, 
                // but for now let's just log it to avoid poison message loops if config is wrong.
            }
        }
    }
}
