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
