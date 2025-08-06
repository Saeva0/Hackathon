using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Configuration;
using System.Threading.Tasks;

namespace Hackathon
{
    public static class EmailServisi
    {
        private static readonly string _emailAdresi = ConfigurationManager.AppSettings["SenderEmailAddress"];
        private static readonly string _emailSifresi = ConfigurationManager.AppSettings["SenderEmailPassword"];
        private static readonly string _smtpHost = "smtp.gmail.com";
        private static readonly int _smtpPort = 587;

        public static async Task GonderSifirlamaKoduAsync(string aliciEmail, string kod)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Hackathon Şifre Sıfırlama", _emailAdresi));
            message.To.Add(new MailboxAddress("", aliciEmail));
            message.Subject = "Şifre Sıfırlama Kodu";

            message.Body = new TextPart("plain")
            {
                Text = $"Merhaba,\n\nŞifrenizi sıfırlamak için doğrulama kodunuz: {kod}\n\nBu kodu siz istemediyseniz, bu e-postayı görmezden gelin.\n\nTeşekkürler."
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(_emailAdresi, _emailSifresi);

                await client.SendAsync(message);

                await client.DisconnectAsync(true);
            }
        }
    }
}