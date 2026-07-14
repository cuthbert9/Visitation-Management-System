using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using VisitorManagementSystem.Api.Configuration;

namespace VisitorManagementSystem.Api.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IOptions<SmtpSettings> smtpOptions)
    {
        _smtpSettings = smtpOptions.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = body
        };

        using var smtpClient = new SmtpClient();
        var socketOptions = _smtpSettings.EnableSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await smtpClient.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, socketOptions);

        if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
        {
            await smtpClient.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
        }

        await smtpClient.SendAsync(message);
        await smtpClient.DisconnectAsync(true);
    }
}
