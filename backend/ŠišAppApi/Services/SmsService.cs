using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ŠišAppApi.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string toPhoneNumber, string message);
    }

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;

        public SmsService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SendSmsAsync(string toPhoneNumber, string message)
        {
            if (string.IsNullOrEmpty(toPhoneNumber))
                throw new ArgumentException("Broj telefona ne može biti prazan.", nameof(toPhoneNumber));

            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Poruka ne može biti prazna.", nameof(message));

            // Provjeri konfiguraciju
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromPhoneNumber = _configuration["Twilio:PhoneNumber"];

            if (string.IsNullOrEmpty(accountSid))
                throw new InvalidOperationException("AccountSid nije konfiguriran u appsettings.json");
            if (string.IsNullOrEmpty(authToken))
                throw new InvalidOperationException("AuthToken nije konfiguriran u appsettings.json");
            if (string.IsNullOrEmpty(fromPhoneNumber))
                throw new InvalidOperationException("PhoneNumber nije konfiguriran u appsettings.json");

            try
            {
                Console.WriteLine($"Inicijalizacija Twilio klijenta...");
                TwilioClient.Init(accountSid, authToken);

                Console.WriteLine($"Slanje SMS-a na broj: {toPhoneNumber}");
                var messageOptions = new CreateMessageOptions(new PhoneNumber(toPhoneNumber))
                {
                    From = new PhoneNumber(fromPhoneNumber),
                    Body = message
                };

                var result = await MessageResource.CreateAsync(messageOptions);
                Console.WriteLine($"SMS uspješno poslan. SID: {result.Sid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom slanja SMS-a: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }
    }
} 