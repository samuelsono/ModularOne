namespace CarTrack.Modules.Leave;

public static class LeaveTypeGenderEligibility
{
    public const string Any = "Any";
    public const string Male = "Male";
    public const string Female = "Female";

    public static string Normalize(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "male" => Male,
            "female" => Female,
            "any" => Any,
            _ => throw new InvalidOperationException(
                $"Eligible gender must be one of: {Any}, {Male}, or {Female}."),
        };

    public static bool IsEligible(string eligibleGender, string? staffGender) =>
        eligibleGender.Equals(Any, StringComparison.OrdinalIgnoreCase)
        || eligibleGender.Equals(staffGender, StringComparison.OrdinalIgnoreCase);
}
