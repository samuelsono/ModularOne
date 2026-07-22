namespace CarTrack.Modules.Leave;

public static class LeaveDayPortions
{
    public const string Full = "Full";
    public const string Half = "Half";

    public static bool IsValid(string? value) =>
        string.Equals(value, Full, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Half, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? value) =>
        string.Equals(value, Half, StringComparison.OrdinalIgnoreCase) ? Half : Full;
}

public static class LeaveAccrualMethods
{
    public const string Upfront = "Upfront";
    public const string Monthly = "Monthly";

    public static bool IsValid(string? value) =>
        string.Equals(value, Upfront, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Monthly, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? value) =>
        string.Equals(value, Monthly, StringComparison.OrdinalIgnoreCase) ? Monthly : Upfront;
}

public static class LeaveTypeCodes
{
    public const string Annual = "ANNUAL";
    public const string Sick = "SICK";
}

public static class WorkLocationCodes
{
    public const string Office = "OFFICE";
    public const string WorkFromHome = "WFH";
    public const string ClientSite = "CLIENT";
    public const string BusinessTravel = "TRAVEL";
    public const string Other = "OTHER";
}

public static class AttendanceSources
{
    public const string Self = "Self";
    public const string Manager = "Manager";
    public const string Hr = "Hr";
    public const string Admin = "Admin";
}

public static class PlannedAttendanceKinds
{
    public const string Work = "Work";
    public const string OnLeave = "OnLeave";
    public const string Unscheduled = "Unscheduled";
}

public enum LeaveRemainingCategory
{
    Annual,
    Sick,
    Other,
}

public static class LeaveRemainingCategoryHelper
{
    public static LeaveRemainingCategory FromLeaveTypeCode(string? leaveTypeCode) =>
        leaveTypeCode?.Trim().ToUpperInvariant() switch
        {
            LeaveTypeCodes.Annual => LeaveRemainingCategory.Annual,
            LeaveTypeCodes.Sick => LeaveRemainingCategory.Sick,
            _ => LeaveRemainingCategory.Other,
        };
}
