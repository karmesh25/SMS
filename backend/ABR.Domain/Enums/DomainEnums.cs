namespace ABR.Domain.Enums;

public enum UserRole
{
    SuperAdmin = 1,
    Admin = 2,
    OfficeStaff = 3,
    ViewOnly = 4
}

public enum FlatStatus
{
    Available = 1,
    Booked = 2,
    Cancelled = 3
}

public enum ConditionType
{
    Auto = 1,
    Manual = 2
}

public enum BookingStatus
{
    Active = 1,
    Cancelled = 2,
    Completed = 3
}

public enum CustomerType
{
    Real = 1,
    Investor = 2
}

public enum EntryType
{
    Aavak = 1,
    Javak = 2
}

public enum AuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3
}
