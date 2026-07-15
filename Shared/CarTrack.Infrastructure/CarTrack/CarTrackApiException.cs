namespace CarTrack.Server.CarTrack;

public class CarTrackApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
