using CarTrack.Core;

namespace CarTrack.Modules.Fleet;

public class Driver : IAuditable
{
    public Guid Id { get; set; }

    public required string DriverCode { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string WorkEmail { get; set; }

    public required string WorkPhone { get; set; }

    public string? WhatsappNumber { get; set; }

    public string Gender { get; set; } = "Other";

    public required string LicenceNumber { get; set; }

    public required string IdNumber { get; set; }

    public DateOnly? LicenceIssued { get; set; }

    public DateOnly? LicenceExpiry { get; set; }

    public string? LicenceCode { get; set; }

    public bool HasPdp { get; set; }

    public string? UnitName { get; set; }

    public string? StreetNumber { get; set; }

    public string? StreetName { get; set; }

    public string? Suburb { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? PostalCode { get; set; }

    public string? EmployeeNumber { get; set; }

    public string? JobTitle { get; set; }

    public string? Department { get; set; }

    public string? Branch { get; set; }

    public string EmploymentType { get; set; } = "Full-time";

    public string EmploymentStatus { get; set; } = "Active";

    public DateOnly? WorkStartDate { get; set; }

    public string? Manager { get; set; }

    public string? CarTrackDriverId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }
}
