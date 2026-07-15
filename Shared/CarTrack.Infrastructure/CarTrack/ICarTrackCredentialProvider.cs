namespace CarTrack.Server.CarTrack;

public record CarTrackRuntimeCredentials(string Username, string Password, string BaseUrl);

public interface ICarTrackCredentialProvider
{
    Task<CarTrackRuntimeCredentials> GetAsync(CancellationToken cancellationToken = default);
}
