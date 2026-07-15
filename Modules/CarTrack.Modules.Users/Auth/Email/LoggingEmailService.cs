namespace CarTrack.Modules.Users;

public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email (dev log) To={To} Subject={Subject} Body={Body}",
            toAddress,
            subject,
            htmlBody);

        return Task.CompletedTask;
    }
}
