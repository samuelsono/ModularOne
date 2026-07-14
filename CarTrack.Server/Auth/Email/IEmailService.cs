namespace CarTrack.Server.Auth.Email;

public interface IEmailService
{
    Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
