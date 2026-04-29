using MassTransit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MimeKit;
using ŠišApp.Contracts;

namespace ŠišApp.Contracts
{
    public class SendEmailEvent
    {
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}

namespace ŠišApp.Helper.Consumers
{
    public class EmailConsumer : IConsumer<SendEmailEvent>
    {
        private readonly ILogger<EmailConsumer> _logger;
        private readonly IConfiguration _configuration;

        public EmailConsumer(ILogger<EmailConsumer> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Consume(ConsumeContext<SendEmailEvent> context)
        {
            _logger.LogInformation($"Received email request for: {context.Message.ToEmail}");

            try
            {
                var message = new MimeMessage();
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");

                message.From.Add(new MailboxAddress("ŠišApp", senderEmail));
                message.To.Add(new MailboxAddress("", context.Message.ToEmail));
                message.Subject = context.Message.Subject;

                message.Body = new TextPart("html")
                {
                    Text = context.Message.Body
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtpServer, smtpPort, false);
                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Email sent successfully to {context.Message.ToEmail}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                throw;
            }
        }
    }
}
