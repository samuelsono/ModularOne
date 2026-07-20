namespace CarTrack.Server.Users;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string SystemAdmin = "SystemAdmin";
    public const string FleetAdmin = "FleetAdmin";
    public const string FleetOperator = "FleetOperator";
    public const string Driver = "Driver";
    public const string Staff = "Staff";
    public const string Manager = "Manager";
    public const string Finance = "Finance";
    public const string Hr = "HR";

    public static readonly IReadOnlyList<string> All =
    [
        Admin,
        SystemAdmin,
        FleetAdmin,
        FleetOperator,
        Driver,
        Staff,
        Manager,
        Finance,
        Hr,
    ];
}

public record PermissionDefinition(
    string Key,
    string ModuleSlug,
    string SubmoduleSlug,
    string Action,
    string Description);

public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionDefinition> All = BuildAll();

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissionKeys { get; } =
        BuildRoleMappings();

    private static List<PermissionDefinition> BuildAll()
    {
        var permissions = new List<PermissionDefinition>();
        permissions.AddRange(Module("platform", [
            ("settings", "Platform settings"),
            ("settings.users", "User management"),
            ("help", "Help centre"),
            ("notifications", "Notifications"),
            ("support", "Support"),
        ]));
        permissions.AddRange(Module("fleet", [
            ("dashboard", "Dashboard"),
            ("tracking", "Live tracking"),
            ("vehicles", "Vehicles"),
            ("drivers", "Drivers"),
            ("reports", "Reports"),
        ]));
        permissions.AddRange(Module("accounting", [
            ("billing", "Billing"),
            ("invoicing", "Invoicing"),
            ("receivables", "Receivables"),
            ("settings", "Accounting settings"),
        ]));
        permissions.AddRange(Module("leave", [
            ("requests", "Leave requests"),
            ("approvals", "Leave approvals"),
            ("calendar", "Team calendar"),
            ("policies", "Leave policies"),
            ("balances", "Leave balances"),
            ("reports", "Leave reports"),
        ]));
        permissions.AddRange(Module("core", [
            ("companies", "Companies"),
            ("departments", "Departments"),
            ("positions", "Positions"),
            ("employees", "Employees"),
        ]));
        permissions.AddRange(Module("expense", [
            ("claims", "Expense claims"),
            ("approvals", "Expense approvals"),
            ("categories", "Expense categories"),
            ("reports", "Expense reports"),
            ("settings", "Expense settings"),
        ]));
        permissions.AddRange(Module("payroll", [
            ("runs", "Payroll runs"),
            ("payslips", "Payslips"),
            ("deductions", "Deductions"),
            ("settings", "Payroll settings"),
        ]));
        permissions.AddRange(Module("performance", [
            ("reviews", "Performance reviews"),
            ("goals", "Goals"),
            ("feedback", "Feedback"),
            ("reports", "Performance reports"),
        ]));
        permissions.AddRange(Module("recruitment", [
            ("jobs", "Job posts"),
            ("applications", "Applications"),
            ("interviews", "Interviews"),
            ("offers", "Offers"),
        ]));
        permissions.AddRange(Module("tenders", [
            ("sources", "Tender sources"),
            ("queries", "Tender keyword queries"),
            ("results", "Tender results"),
            ("runs", "Tender scrape runs"),
        ]));
        return permissions;
    }

    private static IEnumerable<PermissionDefinition> Module(
        string moduleSlug,
        IReadOnlyList<(string Submodule, string Label)> submodules)
    {
        foreach (var (submodule, label) in submodules)
        {
            yield return new PermissionDefinition(
                $"{moduleSlug}.{submodule}.read",
                moduleSlug,
                submodule,
                "read",
                $"Read {label}");

            yield return new PermissionDefinition(
                $"{moduleSlug}.{submodule}.write",
                moduleSlug,
                submodule,
                "write",
                $"Manage {label}");
        }
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildRoleMappings()
    {
        var allKeys = All.Select(permission => permission.Key).ToList();
        var fleetKeys = KeysForModule("fleet");
        // Platform reads for day-to-day roles — exclude Users & Access (Admin/HR only).
        var platformReadKeys = KeysForModule("platform", "read")
            .Where(key => !key.StartsWith("platform.settings.users.", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var platformSettingsCore = KeysMatching("platform.settings.read", "platform.settings.write");
        // Staff/Driver get dashboard (reports.read) for self-only home pages; managers also get approvals.
        var leaveStaff = KeysMatching("leave.requests", "leave.calendar", "leave.balances.read")
            .Concat(KeysMatching("leave.reports.read"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var leaveManager = leaveStaff
            .Concat(KeysMatching("leave.approvals", "leave.reports"))
            .Distinct()
            .ToList();
        var expenseStaff = KeysMatching("expense.claims")
            .Concat(KeysMatching("expense.reports.read", "expense.settings.read"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        // Managers approve expenses and view employees that report to them — not Company/Dept/Positions.
        var expenseManager = expenseStaff
            .Concat(KeysMatching("expense.approvals"))
            .Concat(["core.employees.read"])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var financeKeys = KeysForModule("accounting")
            .Concat(KeysForModule("payroll"))
            .Concat(KeysMatching("expense.approvals", "expense.reports.read", "expense.claims.read", "expense.settings.read", "expense.settings.write"))
            .Distinct()
            .ToList();
        var hrKeys = KeysForModule("leave")
            .Concat(KeysForModule("core"))
            .Concat(KeysForModule("expense"))
            .Concat(KeysForModule("recruitment"))
            .Concat(KeysForModule("performance"))
            .Concat(KeysForModule("tenders"))
            .Concat(KeysMatching("platform.settings.users.read", "platform.settings.users.write"))
            .Distinct()
            .ToList();
        var driverKeys = KeysMatching(
                "fleet.tracking.read",
                "fleet.dashboard.read",
                "leave.requests.read",
                "leave.requests.write",
                "leave.reports.read",
                "leave.calendar.read",
                "leave.balances.read",
                "expense.claims.read",
                "expense.claims.write",
                "expense.reports.read",
                "payroll.payslips.read")
            .ToList();

            expenseManager = expenseManager.Concat(KeysMatching("expense.settings.read")).Distinct().ToList();
            hrKeys = hrKeys.Concat(KeysMatching("expense.settings.read", "expense.settings.write")).Distinct().ToList();

        return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoles.SystemAdmin] = allKeys,
            [AppRoles.Admin] = allKeys,
            [AppRoles.FleetAdmin] = fleetKeys.Concat(platformSettingsCore).Distinct().ToList(),
            [AppRoles.FleetOperator] = fleetKeys,
            [AppRoles.Driver] = driverKeys,
            [AppRoles.Staff] = platformReadKeys
                .Concat(leaveStaff)
                .Concat(expenseStaff)
                .Concat(KeysMatching("payroll.payslips.read"))
                .Distinct()
                .ToList(),
            [AppRoles.Manager] = platformReadKeys
                .Concat(leaveManager)
                .Concat(expenseManager)
                .Concat(KeysMatching("performance.reviews.read", "performance.feedback.read"))
                .Distinct()
                .ToList(),
            [AppRoles.Finance] = platformReadKeys.Concat(financeKeys).Distinct().ToList(),
            [AppRoles.Hr] = platformReadKeys.Concat(hrKeys).Distinct().ToList(),
        };
    }

    private static List<string> KeysForModule(string moduleSlug, string? action = null) =>
        All
            .Where(permission =>
                permission.ModuleSlug.Equals(moduleSlug, StringComparison.OrdinalIgnoreCase)
                && (action is null || permission.Action.Equals(action, StringComparison.OrdinalIgnoreCase)))
            .Select(permission => permission.Key)
            .ToList();

    private static List<string> KeysMatching(params string[] prefixes) =>
        All
            .Where(permission => prefixes.Any(prefix =>
                permission.Key.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase)
                || permission.Key.Equals(prefix + ".read", StringComparison.OrdinalIgnoreCase)
                || permission.Key.Equals(prefix + ".write", StringComparison.OrdinalIgnoreCase)
                || permission.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(permission => permission.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
