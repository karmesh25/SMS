namespace ABR.Application.Common;

public static class AppModules
{
    public const string Dashboard = "dashboard";
    public const string Sites = "sites";
    public const string Wings = "wings";
    public const string Conditions = "conditions";
    public const string Ledgers = "ledgers";
    public const string Banks = "banks";
    public const string Brokers = "brokers";
    public const string Users = "users";
    public const string Devices = "devices";
    public const string Booking = "booking";
    public const string DailyEntry = "daily_entry";
    public const string Dastavej = "dastavej";
    public const string Vyaj = "vyaj";
    public const string Reports = "reports";

    public static readonly string[] All =
    [
        Dashboard, Sites, Wings, Conditions, Ledgers, Banks, Brokers,
        Users, Devices, Booking, DailyEntry, Dastavej, Vyaj, Reports
    ];

    public static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        [Dashboard] = "Dashboard",
        [Sites] = "Sites",
        [Wings] = "Wings",
        [Conditions] = "Conditions",
        [Ledgers] = "Ledgers",
        [Banks] = "Banks",
        [Brokers] = "Brokers",
        [Users] = "Users",
        [Devices] = "Devices",
        [Booking] = "Booking",
        [DailyEntry] = "Daily Entry",
        [Dastavej] = "Dastavej",
        [Vyaj] = "Vyaj Khata",
        [Reports] = "Reports"
    };
}

public static class SystemRoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string OfficeStaff = "OfficeStaff";
    public const string ViewOnly = "ViewOnly";

    public static readonly string[] All = [SuperAdmin, Admin, OfficeStaff, ViewOnly];
}

public enum PermissionLevel
{
    View,
    Manage
}
