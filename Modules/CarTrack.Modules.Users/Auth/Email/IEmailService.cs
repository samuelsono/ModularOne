namespace CarTrack.Modules.Users;

public interface IEmailService
{
    Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
