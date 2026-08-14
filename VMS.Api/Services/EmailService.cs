using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using VisitorManagementSystem.Api.Configuration;

namespace VisitorManagementSystem.Api.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> smtpOptions, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Preparing email to {Recipient} with subject \"{Subject}\".", to, subject);

        ValidateConfiguration();

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

        _logger.LogInformation(
            "Connecting to SMTP {Host}:{Port} using {SecureSocketOptions}. Recipient: {Recipient}. Sender: {Sender}.",
            _smtpSettings.Host,
            _smtpSettings.Port,
            socketOptions,
            to,
            _smtpSettings.SenderEmail);

        var stage = "Connect";

        try
        {
            await smtpClient.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, socketOptions);
            _logger.LogInformation("Successfully connected to SMTP server.");

            if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
            {
                stage = "Authenticate";
                _logger.LogInformation("Authenticating SMTP user {Username}.", _smtpSettings.Username);
                await smtpClient.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                _logger.LogInformation("SMTP authentication successful.");
            }
            else
            {
                _logger.LogInformation("SMTP authentication skipped because no Username was configured.");
            }

            stage = "Send";
            _logger.LogInformation("Sending email to {Recipient} with subject \"{Subject}\".", to, subject);
            await smtpClient.SendAsync(message);
            _logger.LogInformation("SMTP server accepted email for {Recipient}.", to);

            stage = "Disconnect";
            _logger.LogInformation("Disconnecting from SMTP server.");
            await smtpClient.DisconnectAsync(true);
            _logger.LogInformation("SMTP send completed for {Recipient}.", to);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Email delivery failed during {Stage} for {Recipient} using SMTP {Host}:{Port}. Exception type: {ExceptionType}. Message: {ExceptionMessage}.",
                stage,
                to,
                _smtpSettings.Host,
                _smtpSettings.Port,
                exception.GetType().Name,
                exception.Message);

            throw;
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
        {
            _logger.LogWarning("SMTP configuration invalid: Host is missing.");
        }

        if (_smtpSettings.Port <= 0)
        {
            _logger.LogWarning("SMTP configuration invalid: Port is {Port}.", _smtpSettings.Port);
        }

        if (string.IsNullOrWhiteSpace(_smtpSettings.SenderEmail))
        {
            _logger.LogWarning("SMTP configuration invalid: SenderEmail is missing.");
        }

        if (string.IsNullOrWhiteSpace(_smtpSettings.Username))
        {
            _logger.LogWarning("SMTP configuration warning: Username is missing; authentication will be skipped.");
        }

        if (string.IsNullOrWhiteSpace(_smtpSettings.Password))
        {
            _logger.LogWarning("SMTP password is missing.");
        }
    }
}
