namespace HrmsApi.Models;

// ── Auth ─────────────────────────────────────────────────────────────────────

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string Username, string Role, DateTime ExpiresAt);

// ── Employee ──────────────────────────────────────────────────────────────────

/// <summary>
/// Payload for creating or updating an employee.
/// Includes new statutory identity fields (IC, Tax, Bank, Department, ProfilePicture).
/// </summary>
public record EmployeeRequest(
    string   Name,
    string   Email,
    string   Phone,
    int?     DepartmentId,      // Changed from Department string to DepartmentId FK
    string   Designation,
    DateTime JoinDate,
    decimal  Salary,
    bool     IsActive,
    string   IcPassport,
    string   TaxNumber,
    int?     BankId,
    string   AccountNumber,
    string   ProfilePicture     // New field for profile picture URL/path
);

// ── Attendance ────────────────────────────────────────────────────────────────

public record AttendanceRequest(
    int       EmployeeId,
    DateTime  Date,
    string    Status,
    DateTime? CheckIn,
    DateTime? CheckOut,
    string    Remarks
);

// ── Payroll ───────────────────────────────────────────────────────────────────

public record PayrollRequest(
    int     EmployeeId,
    int     Month,
    int     Year,
    decimal BasicSalary,
    decimal Allowances,
    decimal Deductions
);

// ── User Management ───────────────────────────────────────────────────────────

public record UserRequest(
    string Username,
    string Password,
    string Role,
    bool   IsActive
);

public record UserUpdateRequest(
    string  Username,
    string  Role,
    bool    IsActive,
    string? Password  // optional — leave blank to keep existing password
);

// ── Bank Master ───────────────────────────────────────────────────────────────

public record BankMasterRequest(
    string Name,
    bool   IsActive
);
