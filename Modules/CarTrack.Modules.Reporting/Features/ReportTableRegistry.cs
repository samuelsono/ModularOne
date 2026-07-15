namespace CarTrack.Modules.Reporting;

public static class ReportTypes
{
    public const string MetricCard = "MetricCard";
    public const string Column = "Column";
    public const string Pie = "Pie";
    public const string Donut = "Donut";
    public const string Line = "Line";
    public const string ApiTable = "ApiTable";
    public const string Map = "Map";

    public static readonly IReadOnlyList<string> All =
    [
        MetricCard,
        Column,
        Pie,
        Donut,
        Line,
        ApiTable,
        Map,
    ];

    public static int MaxGroupByColumns(string reportType) => reportType switch
    {
        MetricCard => 0,
        ApiTable => 0,
        Map => 0,
        Column => 2,
        Pie => 1,
        Donut => 1,
        Line => 2,
        _ => 0,
    };
}

public static class ReportSizes
{
    public const string Small = "Small";
    public const string Medium = "Medium";
    public const string Large = "Large";
    public const string FullWidth = "FullWidth";

    public static readonly IReadOnlyList<string> All =
    [
        Small,
        Medium,
        Large,
        FullWidth,
    ];
}

public static class LayoutDirections
{
    public const string Row = "Row";
    public const string Column = "Column";

    public static readonly IReadOnlyList<string> All = [Row, Column];
}

public static class AggregateFunctions
{
    public const string Count = "Count";
    public const string Sum = "Sum";
    public const string Average = "Average";
    public const string Min = "Min";
    public const string Max = "Max";

    public static readonly IReadOnlyList<string> All =
    [
        Count,
        Sum,
        Average,
        Min,
        Max,
    ];
}

public static class TargetTables
{
    public const string Vehicles = "Vehicles";
    public const string Drivers = "Drivers";
    public const string LeaveRequests = "LeaveRequests";
    public const string LeaveBalances = "LeaveBalances";
    public const string ExpenseClaims = "ExpenseClaims";
    public const string Departments = "Departments";
    public const string StaffProfiles = "StaffProfiles";

    public static readonly IReadOnlyList<string> All =
    [
        Vehicles,
        Drivers,
        LeaveRequests,
        LeaveBalances,
        ExpenseClaims,
        Departments,
        StaffProfiles,
    ];
}

public enum ReportColumnKind
{
    String,
    Number,
    Boolean,
    Date,
    DateTime,
}

public record ReportColumnMetadata(
    string Name,
    string Label,
    ReportColumnKind Kind,
    bool IsGroupable,
    bool IsAggregatable);

public record ReportTableMetadata(
    string Name,
    string Label,
    IReadOnlyList<ReportColumnMetadata> Columns,
    IReadOnlyList<string> AllowedAggregates);

public static class ReportTableRegistry
{
    private static readonly IReadOnlyDictionary<string, ReportTableMetadata> Tables =
        new Dictionary<string, ReportTableMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            [TargetTables.Vehicles] = new(
                TargetTables.Vehicles,
                "Vehicles",
                [
                    Col("Make", "Make", ReportColumnKind.String, groupable: true),
                    Col("Model", "Model", ReportColumnKind.String, groupable: true),
                    Col("FuelType", "Fuel Type", ReportColumnKind.String, groupable: true),
                    Col("VehicleType", "Vehicle Type", ReportColumnKind.String, groupable: true),
                    Col("IgnitionStatus", "Ignition Status", ReportColumnKind.String, groupable: true),
                    Col("Colour", "Colour", ReportColumnKind.String, groupable: true),
                    Col("RegisteredOwner", "Registered Owner", ReportColumnKind.String, groupable: true),
                    Col("Speed", "Speed", ReportColumnKind.Number, aggregatable: true),
                    Col("Odometer", "Odometer", ReportColumnKind.Number, aggregatable: true),
                    Col("FuelLevel", "Fuel Level", ReportColumnKind.Number, aggregatable: true),
                    Col("FuelPercentageLeft", "Fuel % Left", ReportColumnKind.Number, aggregatable: true),
                    Col("Year", "Year", ReportColumnKind.Number, aggregatable: true),
                    Col("Tare", "Tare", ReportColumnKind.Number, aggregatable: true),
                    Col("Gvm", "GVM", ReportColumnKind.Number, aggregatable: true),
                    Col("CreatedAt", "Created At", ReportColumnKind.DateTime, groupable: true),
                ],
                [AggregateFunctions.Count, AggregateFunctions.Sum, AggregateFunctions.Average, AggregateFunctions.Min, AggregateFunctions.Max]),

            [TargetTables.Drivers] = new(
                TargetTables.Drivers,
                "Drivers",
                [
                    Col("Department", "Department", ReportColumnKind.String, groupable: true),
                    Col("Branch", "Branch", ReportColumnKind.String, groupable: true),
                    Col("City", "City", ReportColumnKind.String, groupable: true),
                    Col("Province", "Province", ReportColumnKind.String, groupable: true),
                    Col("EmploymentType", "Employment Type", ReportColumnKind.String, groupable: true),
                    Col("EmploymentStatus", "Employment Status", ReportColumnKind.String, groupable: true),
                    Col("Gender", "Gender", ReportColumnKind.String, groupable: true),
                    Col("LicenceCode", "Licence Code", ReportColumnKind.String, groupable: true),
                    Col("LicenceExpiry", "Licence Expiry", ReportColumnKind.Date, aggregatable: true),
                    Col("WorkStartDate", "Work Start Date", ReportColumnKind.Date, aggregatable: true),
                    Col("CreatedAt", "Created At", ReportColumnKind.DateTime, groupable: true),
                ],
                [AggregateFunctions.Count, AggregateFunctions.Min, AggregateFunctions.Max]),

            // Leave tables
            [TargetTables.LeaveRequests] = new(
                TargetTables.LeaveRequests,
                "Leave Requests",
                [
                    Col("Status", "Status", ReportColumnKind.String, groupable: true),
                    Col("StartDayPortion", "Start Day Portion", ReportColumnKind.String, groupable: true),
                    Col("EndDayPortion", "End Day Portion", ReportColumnKind.String, groupable: true),
                    Col("RequesterBranch", "Branch", ReportColumnKind.String, groupable: true),
                    Col("StartDate", "Start Date", ReportColumnKind.Date, aggregatable: true),
                    Col("EndDate", "End Date", ReportColumnKind.Date, aggregatable: true),
                    Col("CreatedAt", "Created At", ReportColumnKind.DateTime, groupable: true),
                ],
                [AggregateFunctions.Count, AggregateFunctions.Min, AggregateFunctions.Max]),

            [TargetTables.LeaveBalances] = new(
                TargetTables.LeaveBalances,
                "Leave Balances",
                [
                    Col("CycleStart", "Cycle Start", ReportColumnKind.Date, groupable: true),
                    Col("CycleEnd", "Cycle End", ReportColumnKind.Date, aggregatable: true),
                ],
                [AggregateFunctions.Count]),

            // Claims
            [TargetTables.ExpenseClaims] = new(
                TargetTables.ExpenseClaims,
                "Expense Claims",
                [
                    Col("Status", "Status", ReportColumnKind.String, groupable: true),
                    Col("Currency", "Currency", ReportColumnKind.String, groupable: true),
                    Col("RequesterBranch", "Branch", ReportColumnKind.String, groupable: true),
                    Col("ExpenseDate", "Expense Date", ReportColumnKind.Date, aggregatable: true),
                    Col("CreatedAt", "Created At", ReportColumnKind.DateTime, groupable: true),
                ],
                [AggregateFunctions.Count, AggregateFunctions.Min, AggregateFunctions.Max]),

            // Core tables
            [TargetTables.Departments] = new(
                TargetTables.Departments,
                "Departments",
                [
                    Col("Name", "Name", ReportColumnKind.String, groupable: true),
                    Col("CreatedAt", "Created At", ReportColumnKind.DateTime, groupable: true),
                ],
                [AggregateFunctions.Count]),

            [TargetTables.StaffProfiles] = new(
                TargetTables.StaffProfiles,
                "Staff Profiles",
                [
                    Col("Department", "Department", ReportColumnKind.String, groupable: true),
                    Col("Branch", "Branch", ReportColumnKind.String, groupable: true),
                    Col("EmploymentStatus", "Employment Status", ReportColumnKind.String, groupable: true),
                    Col("WorkStartDate", "Work Start Date", ReportColumnKind.Date, aggregatable: true),
                    Col("CreatedAt", "Created At", ReportColumnKind.DateTime, groupable: true),
                ],
                [AggregateFunctions.Count, AggregateFunctions.Min, AggregateFunctions.Max]),
        };

    public static IReadOnlyList<ReportTableMetadata> GetAllTables() => Tables.Values.ToList();

    public static ReportTableMetadata? GetTable(string tableName) =>
        Tables.TryGetValue(tableName, out var table) ? table : null;

    public static ReportColumnMetadata? GetColumn(string tableName, string columnName) =>
        GetTable(tableName)?.Columns.FirstOrDefault(column =>
            column.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

    private static ReportColumnMetadata Col(
        string name,
        string label,
        ReportColumnKind kind,
        bool groupable = false,
        bool aggregatable = false) =>
        new(name, label, kind, groupable, aggregatable);
}
