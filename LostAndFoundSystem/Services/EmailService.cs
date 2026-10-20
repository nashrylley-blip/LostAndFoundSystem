using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace LostAndFoundSystem.Services
{
    /// <summary>
    /// Values come from the "Smtp" section of appsettings.json / appsettings.Development.json
    /// </summary>
    public class SmtpSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string FromName { get; set; } = "Campus Lost & Found";
        public string Username { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
    }

    public interface IEmailService
    {
        bool IsConfigured { get; }

        Task SendAsync(string toEmail, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.Username) &&
            !string.IsNullOrWhiteSpace(_settings.AppPassword) &&
            !_settings.Username.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.Username, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            // Gmail App Passwords are shown with spaces (xxxx xxxx xxxx xxxx) - remove them
            string password = _settings.AppPassword.Replace(" ", string.Empty);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_settings.Username, password)
            };

            await client.SendMailAsync(message);
        }
    }

    /// <summary>
    /// A registration that is waiting for the e-mail code. Kept in memory for 10 minutes.
    /// </summary>
    public class PendingRegistration
    {
        public string StudentID { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public DateTime LastSentAt { get; set; } = DateTime.Now;
    }
}
