using System.Text.Json;

namespace CarTrack.Server.Reports;

public static class ApiResources
{
    public const string Vehicles = "vehicles";
    public const string VehicleStatus = "vehicle-status";
    public const string Events = "events";
    public const string Trips = "trips";
    public const string Alerts = "alerts";
    public const string AlertNotifications = "alert-notifications";

    public static readonly IReadOnlyList<string> All =
    [
        Vehicles,
        VehicleStatus,
        Events,
        Trips,
        Alerts,
        AlertNotifications,
    ];

    public static bool RequiresRegistration(string resource) =>
        resource.Equals(Events, StringComparison.OrdinalIgnoreCase) ||
        resource.Equals(Trips, StringComparison.OrdinalIgnoreCase);

    public static bool RequiresDateRange(string resource) =>
        resource.Equals(AlertNotifications, StringComparison.OrdinalIgnoreCase);
}

public record ApiTableColumnMapping(string Field, string Label);

public record ApiTableConfig(
    string Endpoint,
    IReadOnlyList<ApiTableColumnMapping> ColumnMappings,
    string? Registration,
    DateTimeOffset? StartTimestamp,
    DateTimeOffset? EndTimestamp);

public static class ApiTableConfigParser
{
    public const int PerPage = 6;

    public static ApiTableConfig Parse(string endpoint, string? chartOptionsJson)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(chartOptionsJson) ? "{}" : chartOptionsJson);
        var root = document.RootElement;

        var mappings = ParseColumnMappings(root);
        var registration = root.TryGetProperty("registration", out var registrationElement)
            ? registrationElement.GetString()
            : null;

        var startTimestamp = TryReadTimestamp(root, "startTimestamp");
        var endTimestamp = TryReadTimestamp(root, "endTimestamp");

        if (ApiResources.RequiresRegistration(endpoint) && string.IsNullOrWhiteSpace(registration))
        {
            throw new InvalidOperationException("Registration is required for this API resource.");
        }

        if (mappings.Count == 0)
        {
            throw new InvalidOperationException("At least one column mapping is required.");
        }

        var now = DateTimeOffset.UtcNow;
        var resolvedStart = startTimestamp ?? now.AddDays(-7);
        var resolvedEnd = endTimestamp ?? now;

        return new ApiTableConfig(
            endpoint,
            mappings,
            registration?.Trim(),
            resolvedStart,
            resolvedEnd);
    }

    private static IReadOnlyList<ApiTableColumnMapping> ParseColumnMappings(JsonElement root)
    {
        if (!root.TryGetProperty("columnMapping", out var mappingElement) ||
            mappingElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var mappings = new List<ApiTableColumnMapping>();
        foreach (var property in mappingElement.EnumerateObject())
        {
            var label = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();

            mappings.Add(new ApiTableColumnMapping(
                property.Name,
                string.IsNullOrWhiteSpace(label) ? property.Name : label));
        }

        return mappings;
    }

    private static DateTimeOffset? TryReadTimestamp(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var valueElement))
        {
            return null;
        }

        var raw = valueElement.ValueKind == JsonValueKind.String
            ? valueElement.GetString()
            : valueElement.ToString();

        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
    }
}

public static class ApiTableValueExtractor
{
    public static object? Extract(JsonElement row, string fieldPath)
    {
        if (string.IsNullOrWhiteSpace(fieldPath))
        {
            return null;
        }

        var current = row;
        foreach (var segment in fieldPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!TryGetProperty(current, segment, out current))
            {
                return null;
            }
        }

        return FormatValue(current);
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
        {
            return true;
        }

        var snakeCase = ToSnakeCase(name);
        if (!string.Equals(snakeCase, name, StringComparison.Ordinal) &&
            element.TryGetProperty(snakeCase, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var chars = new List<char>(value.Length + 4);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
            {
                chars.Add('_');
            }

            chars.Add(char.ToLowerInvariant(character));
        }

        return new string(chars.ToArray());
    }

    private static object? FormatValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => value.TryGetInt64(out var integer)
                ? integer
                : value.GetDouble(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Array => string.Join(", ", value.EnumerateArray().Select(item => FormatValue(item)?.ToString() ?? string.Empty)),
            JsonValueKind.Object => value.GetRawText(),
            _ => value.ToString(),
        };
}
