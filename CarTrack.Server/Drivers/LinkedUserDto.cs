namespace CarTrack.Server.Drivers;

public record LinkedUserDto(
    string UserId,
    string Username,
    string Email,
    string? DisplayName,
    bool IsActive);
