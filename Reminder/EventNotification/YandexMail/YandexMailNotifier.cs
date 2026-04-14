using System.Net;
using System.Net.Mail;

namespace ReminderApp.EventNotification.YandexMail;

public class YandexMailNotifier : INotifier
{
    private readonly string _email;
    private readonly string _password;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly bool _enableSsl;
    private readonly string? _toEmail;

    public YandexMailNotifier(IYandexMailCredentialsProvider credentialsProvider)
    {
        var settings = credentialsProvider.GetCredentials();
        
        _email = settings.Email;
        _password = settings.Password;
        _smtpHost = settings.SmtpHost;
        _smtpPort = settings.SmtpPort;
        _enableSsl = settings.EnableSsl;
        _toEmail = settings.ToEmail;
    }

    public async Task NotifyAsync(string message)
    {
        if (string.IsNullOrEmpty(_toEmail))
        {
            Log.Information("❌ Yandex Mail: Email получателя не указан");
            return;
        }

        try
        {
            var success = await SendEmailAsync(_toEmail, message);
            
            if (success)
            {
                Log.Information($"✅ Email отправлен через Yandex Mail на {_toEmail}");
            }
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Ошибка отправки Email: {ex.Message}");
        }
    }

    private async Task<bool> SendEmailAsync(string to, string text)
    {
        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = _enableSsl,
                Credentials = new NetworkCredential(_email, _password),
                UseDefaultCredentials = false,
                Timeout = 30000
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_email, "Reminder"),
                Subject = "Напоминание",
                Body = text,
                IsBodyHtml = false
            };
            
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Yandex Mail API error: {ex.Message}");
            return false;
        }
    }

}