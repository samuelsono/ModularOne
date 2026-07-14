using CarTrack.Server.Data;

namespace CarTrack.Server.Drivers;

public static class DriverMapper
{
    public static DriverDto ToDto(Driver driver, LinkedUserDto? linkedUser = null) => new(
        Id: driver.Id.ToString(),
        DriverId: driver.DriverCode,
        Name: $"{driver.FirstName} {driver.LastName}".Trim(),
        Email: driver.WorkEmail,
        ContactNumber: driver.WorkPhone,
        Gender: driver.Gender,
        Department: driver.Department ?? string.Empty,
        LicenceNumber: driver.LicenceNumber,
        LicenceIssued: driver.LicenceIssued?.ToString("yyyy-MM-dd"),
        LicenceExpiry: driver.LicenceExpiry?.ToString("yyyy-MM-dd"),
        FirstName: driver.FirstName,
        LastName: driver.LastName,
        WorkEmail: driver.WorkEmail,
        WorkPhone: driver.WorkPhone,
        WhatsappNumber: driver.WhatsappNumber,
        IdNumber: driver.IdNumber,
        LicenceCode: driver.LicenceCode,
        HasPdp: driver.HasPdp,
        UnitName: driver.UnitName,
        StreetNumber: driver.StreetNumber,
        StreetName: driver.StreetName,
        Suburb: driver.Suburb,
        City: driver.City,
        Province: driver.Province,
        PostalCode: driver.PostalCode,
        EmployeeNumber: driver.EmployeeNumber,
        JobTitle: driver.JobTitle,
        Branch: driver.Branch,
        EmploymentType: driver.EmploymentType,
        EmploymentStatus: driver.EmploymentStatus,
        WorkStartDate: driver.WorkStartDate?.ToString("yyyy-MM-dd"),
        Manager: driver.Manager,
        CreatedAt: AuditableMapping.FormatTimestamp(driver.CreatedAt),
        CreatedByUserId: driver.CreatedByUserId,
        CreatedByDisplayName: AuditableMapping.UserDisplayName(driver.CreatedByUser),
        UpdatedAt: AuditableMapping.FormatTimestamp(driver.UpdatedAt == default ? driver.CreatedAt : driver.UpdatedAt),
        UpdatedByUserId: driver.UpdatedByUserId,
        UpdatedByDisplayName: AuditableMapping.UserDisplayName(driver.UpdatedByUser),
        LinkedUser: linkedUser);

    public static void ApplyRequest(Driver driver, SaveDriverRequest request)
    {
        driver.FirstName = request.FirstName.Trim();
        driver.LastName = request.LastName.Trim();
        driver.WorkEmail = request.WorkEmail.Trim();
        driver.WorkPhone = request.WorkPhone.Trim();
        driver.WhatsappNumber = request.WhatsappNumber?.Trim();
        driver.Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Other" : request.Gender.Trim();
        driver.LicenceNumber = request.LicenceNumber.Trim();
        driver.IdNumber = request.IdNumber.Trim();
        driver.LicenceIssued = ParseDateOnly(request.LicenceIssued);
        driver.LicenceExpiry = ParseDateOnly(request.LicenceExpiry);
        driver.LicenceCode = request.LicenceCode?.Trim();
        driver.HasPdp = request.HasPdp;
        driver.UnitName = request.UnitName?.Trim();
        driver.StreetNumber = request.StreetNumber?.Trim();
        driver.StreetName = request.StreetName?.Trim();
        driver.Suburb = request.Suburb?.Trim();
        driver.City = request.City?.Trim();
        driver.Province = request.Province?.Trim();
        driver.PostalCode = request.PostalCode?.Trim();
        driver.EmployeeNumber = request.EmployeeNumber?.Trim();
        driver.JobTitle = request.JobTitle?.Trim();
        driver.Department = request.Department?.Trim();
        driver.Branch = request.Branch?.Trim();
        driver.EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType)
            ? "Full-time"
            : request.EmploymentType.Trim();
        driver.EmploymentStatus = string.IsNullOrWhiteSpace(request.EmploymentStatus)
            ? "Active"
            : request.EmploymentStatus.Trim();
        driver.WorkStartDate = ParseDateOnly(request.WorkStartDate);
        driver.Manager = request.Manager?.Trim();
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParse(value, out var date) ? date : null;
    }
}
