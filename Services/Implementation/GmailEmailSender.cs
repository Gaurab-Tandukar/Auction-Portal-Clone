using System.Net;
using System.Net.Mail;
using Auction_Portal_Clone.Services.Interfaces;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class GmailEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public GmailEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var fromAddress = _config["EmailSettings:GmailAddress"];
            var appPassword = _config["EmailSettings:GmailAppPassword"];

            if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(appPassword))
                throw new InvalidOperationException(
                    "Email sender is not configured. Set EmailSettings:GmailAddress and " +
                    "EmailSettings:GmailAppPassword (e.g. via dotnet user-secrets).");

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress, appPassword)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, "Siddhartha Bank Auction Portal"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await smtp.SendMailAsync(message);
        }
    }
}