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

        public EmailService(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            await _publishEndpoint.Publish(new SendEmailEvent
            {
                ToEmail = toEmail,
                Subject = subject,
                Body = body
            });
        }
    }
}