using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Catrashop.Modules.Notifications.Infrastructure.Email;

/// <summary>
/// Development email "sender": it logs the message instead of calling a real provider. Swapping in an SMTP or
/// transactional-email provider later is just another <see cref="IEmailSender"/> registration — no caller changes.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation("[email] To={To} Subject=\"{Subject}\" Body=\"{Body}\"",
            message.ToEmail, message.Subject, message.Body);
        return Task.FromResult(Result.Success());
    }
}
