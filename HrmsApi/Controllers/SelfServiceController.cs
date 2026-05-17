using HrmsApi.Data;
using HrmsApi.Models;
using HrmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ClosedXML.Excel;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SelfServiceController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly ILogger<SelfServiceController> _logger;
    private readonly ITimesheetService _timesheetService;
    private readonly IEmailService _emailService;
    private readonly IPdfService _pdfService;
    private readonly IServiceScopeFactory _scopeFactory;

    public SelfServiceController(
        HrmsDbContext db,
        ILogger<SelfServiceController> logger,
        ITimesheetService timesheetService,
        IEmailService emailService,
        IPdfService pdfService,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _logger = logger;
        _timesheetService = timesheetService;
        _emailService = emailService;
        _pdfService = pdfService;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// <summary>
    /// GET /api/selfservice/check-link - Debug: Check user-employee link
    /// </summary>
    [HttpGet("check-link")]
    public async Task<IActionResult> CheckUserEmployeeLink()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        _logger.LogInformation("Checking employee link for username: {Username}", username);

        // Find user account
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        // Find employee by exact email match
        var employeeExact = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        // Find employee by case-insensitive email match
        var employeeCaseInsensitive = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == username.ToLower());

        // Get all employees with similar email
        var similarEmployees = await _db.Employees
            .Where(e => e.Email.ToLower().Contains(username.ToLower()) || username.ToLower().Contains(e.Email.ToLower()))
            .Select(e => new { e.Id, e.Email, e.Name, e.IsActive })
            .ToListAsync();

        // Check for duplicate usernames
        var duplicateUsers = await _db.Users
            .Where(u => u.Username == username)
            .Select(u => new { u.Id, u.Username, u.Role, u.IsActive })
            .ToListAsync();

        return Ok(new
        {
            loggedInAs = username,
            userAccount = user != null ? new { user.Id, user.Username, user.Role, user.IsActive } : null,
            employeeFoundExact = employeeExact != null,
            employeeExact = employeeExact != null ? new { employeeExact.Id, employeeExact.Email, employeeExact.Name, employeeExact.IsActive } : null,
            employeeFoundCaseInsensitive = employeeCaseInsensitive != null,
            employeeCaseInsensitive = employeeCaseInsensitive != null ? new { employeeCaseInsensitive.Id, employeeCaseInsensitive.Email, employeeCaseInsensitive.Name, employeeCaseInsensitive.IsActive } : null,
            similarEmployees = similarEmployees,
            duplicateUserAccounts = duplicateUsers,
            hasDuplicates = duplicateUsers.Count > 1,
            recommendation = employeeExact == null 
                ? "No employee found with exact email match. Create employee with email: " + username
                : employeeExact.IsActive 
                    ? "Employee found and active. Link should work."
                    : "Employee found but INACTIVE. Activate employee to use self-service."
        });
    }


    /// GET /api/selfservice/my-profile - Get current logged-in user's employee profile
    /// </summary>
    [HttpGet("my-profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user to get their employee record
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        // Find employee by email (username is the email)
        var employee = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Bank)
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found. Please contact HR." });

        return Ok(new
        {
            employee.Id,
            employee.Name,
            employee.Email,
            employee.Phone,
            employee.IcPassport,
            employee.TaxNumber,
            departmentId = employee.DepartmentId,
            departmentName = employee.Department?.Name,
            employee.Designation,
            employee.JoinDate,
            employee.Salary,
            bankId = employee.BankId,
            bankName = employee.Bank?.Name,
            employee.AccountNumber,
            employee.ProfilePicture,
            employee.IsActive,
            employee.CreatedAt
        });
    }

    /// <summary>
    /// PUT /api/selfservice/my-profile - Update current user's profile (limited fields)
    /// </summary>
    [HttpPost("my-profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateSelfProfileRequest request)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        // Allow employees to update only specific fields
        if (!string.IsNullOrWhiteSpace(request.Phone))
            employee.Phone = request.Phone;

        if (!string.IsNullOrWhiteSpace(request.Email))
            employee.Email = request.Email;

        // Bank details update
        if (request.BankId.HasValue && request.BankId.Value > 0)
            employee.BankId = request.BankId;

        if (!string.IsNullOrWhiteSpace(request.AccountNumber))
            employee.AccountNumber = request.AccountNumber;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully", employee });
    }
    /// <summary>
    /// GET /api/selfservice/dashboard - Get employee dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetEmployeeDashboard()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;

        // 1. Leave Balance Summary
        var leaveBalances = await _db.EmployeeLeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == employee.Id && lb.Year == currentYear)
            .Select(lb => new
            {
                leaveTypeCode = lb.LeaveType.Code,
                leaveTypeName = lb.LeaveType.Name,
                lb.TotalDays,
                lb.UsedDays,
                lb.BalanceDays,
                lb.CarryForwardDays
            })
            .ToListAsync();

        // 2. Pending Leave Requests Count
        var pendingLeaveCount = await _db.LeaveRequests
            .Where(lr => lr.EmployeeId == employee.Id && lr.Status == "Pending")
            .CountAsync();

        // 3. Pending Attendance Periods Count
        var pendingAttendanceCount = await _db.AttendancePeriods
            .Where(ap => ap.EmployeeId == employee.Id && ap.Status == "Submitted")
            .CountAsync();

        // 4. Recent Payslips (Last 3 months)
        var recentPayslips = await _db.Payrolls
            .Where(p => p.EmployeeId == employee.Id)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Take(3)
            .Select(p => new
            {
                p.Id,
                p.Month,
                p.Year,
                monthYear = new DateTime(p.Year, p.Month, 1).ToString("MMM yyyy"),
                p.NetSalary,
                p.Status,
                p.GeneratedOn
            })
            .ToListAsync();

        // 5. Upcoming Leaves (Approved leaves in next 30 days)
        var today = DateTime.UtcNow.Date;
        var next30Days = today.AddDays(30);
        var upcomingLeaves = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == employee.Id 
                      && lr.Status == "Approved" 
                      && lr.StartDate >= today 
                      && lr.StartDate <= next30Days)
            .OrderBy(lr => lr.StartDate)
            .Select(lr => new
            {
                lr.Id,
                leaveType = lr.LeaveType.Name,
                leaveTypeCode = lr.LeaveType.Code,
                lr.StartDate,
                lr.EndDate,
                lr.TotalDays
            })
            .ToListAsync();

        // 6. Recent Attendance Periods (Last 3 periods)
        var recentAttendance = await _db.AttendancePeriods
            .Where(ap => ap.EmployeeId == employee.Id)
            .OrderByDescending(ap => ap.StartDate)
            .Take(3)
            .Select(ap => new
            {
                ap.Id,
                ap.StartDate,
                ap.EndDate,
                ap.Status,
                periodLabel = ap.StartDate.ToString("MMM dd") + " - " + ap.EndDate.ToString("MMM dd, yyyy")
            })
            .ToListAsync();

        // 7. Upcoming Public Holidays (Next 3 months)
        var next3Months = today.AddMonths(3);
        var upcomingHolidays = await _db.PublicHolidays
            .Where(h => h.Date >= today && h.Date <= next3Months)
            .OrderBy(h => h.Date)
            .Take(5)
            .Select(h => new
            {
                h.Id,
                h.Name,
                h.Date,
                dayOfWeek = h.Date.DayOfWeek.ToString()
            })
            .ToListAsync();

        // 8. Quick Stats
        var stats = new
        {
            totalLeaveBalance = leaveBalances.Sum(lb => lb.BalanceDays),
            totalLeaveUsed = leaveBalances.Sum(lb => lb.UsedDays),
            pendingApprovals = pendingLeaveCount + pendingAttendanceCount,
            currentMonthAttendance = await _db.AttendancePeriods
                .Where(ap => ap.EmployeeId == employee.Id 
                          && ap.StartDate.Month == currentMonth 
                          && ap.StartDate.Year == currentYear)
                .CountAsync()
        };

        return Ok(new
        {
            employee = new
            {
                employee.Id,
                employee.Name,
                employee.Email,
                employee.EmployeeCode,
                employee.Designation,
                departmentName = employee.Department?.Name,
                employee.JoinDate
            },
            stats,
            leaveBalances,
            pendingCounts = new
            {
                leaveRequests = pendingLeaveCount,
                attendancePeriods = pendingAttendanceCount,
                total = pendingLeaveCount + pendingAttendanceCount
            },
            recentPayslips,
            upcomingLeaves,
            recentAttendance,
            upcomingHolidays
        });
    }


    /// <summary>
    /// GET /api/selfservice/my-payslips - Get current user's payslips
    /// </summary>
    [HttpGet("my-payslips")]
    public async Task<IActionResult> GetMyPayslips([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var query = _db.Payrolls
            .Where(p => p.EmployeeId == employee.Id);

        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        if (month.HasValue)
            query = query.Where(p => p.Month == month.Value);

        var payslips = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new
            {
                p.Id,
                p.Month,
                p.Year,
                monthName = new DateTime(p.Year, p.Month, 1).ToString("MMMM yyyy"),
                p.BasicSalary,
                p.Allowances,
                p.Deductions,
                p.EpfAmount,
                p.SocsoAmount,
                p.TaxAmount,
                p.GrossIncome,
                p.NetSalary,
                p.GeneratedOn
            })
            .ToListAsync();

        return Ok(payslips);
    }

    /// <summary>
    /// GET /api/selfservice/my-payslips/{id} - Get specific payslip details for current user
    /// </summary>
    [HttpGet("my-payslips/{id:int}")]
    public async Task<IActionResult> GetMyPayslipById(int id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var payslip = await _db.Payrolls
            .Include(p => p.Employee)
            .Include(p => p.Approver)
            .Include(p => p.Processor)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmployeeId == employee.Id);

        if (payslip == null)
            return NotFound(new { message = "Payslip not found or does not belong to you" });

        return Ok(new
        {
            payslip.Id,
            payslip.Month,
            payslip.Year,
            monthName = new DateTime(payslip.Year, payslip.Month, 1).ToString("MMMM yyyy"),
            payslip.BasicSalary,
            payslip.Allowances,
            payslip.Deductions,
            payslip.EpfAmount,
            payslip.SocsoAmount,
            payslip.TaxAmount,
            payslip.GrossIncome,
            payslip.NetSalary,
            payslip.Status,
            payslip.GeneratedOn,
            approverName = payslip.Approver?.Username,
            payslip.ApprovedOn
        });
    }

    /// <summary>
    /// POST /api/selfservice/my-payslips/{id}/email - Request payslip to be emailed
    /// </summary>
    [HttpPost("my-payslips/{id:int}/email")]
    public async Task<IActionResult> EmailMyPayslip(int id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var payroll = await _db.Payrolls
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmployeeId == employee.Id);

        if (payroll == null)
            return NotFound(new { message = "Payslip not found or does not belong to you" });

        if (string.IsNullOrWhiteSpace(employee.Email))
            return BadRequest(new { message = "Your employee profile does not have an email address configured" });

        try
        {
            // Generate PDF attachment
            var pdfData = await _pdfService.GeneratePayslipPdfAsync(id);
            var monthName = new DateTime(payroll.Year, payroll.Month, 1).ToString("MMMM_yyyy");
            var fileName = $"Payslip_{payroll.Employee.Name.Replace(" ", "_")}_{monthName}.pdf";

            var subject = $"Payslip for {new DateTime(payroll.Year, payroll.Month, 1):MMMM yyyy}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #1a1a1a; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; }}
        .header {{ background: #4169E1; color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: normal; }}
        .content {{ padding: 40px 30px; background-color: white; }}
        .content p {{ margin: 10px 0; color: #1a1a1a; font-size: 15px; }}
        .salary-box {{ background: white; padding: 30px; margin: 30px 0; text-align: center; border: 3px solid #e0e0e0; border-left: 6px solid #10b981; }}
        .salary-label {{ margin: 0; font-size: 16px; color: #666; }}
        .salary-amount {{ font-size: 48px; font-weight: bold; color: #10b981; margin: 10px 0; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 13px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🧾 Your Payslip is Ready</h1>
        </div>
        <div class=""content"">
            <p>Dear <strong>{payroll.Employee.Name}</strong>,</p>
            <p>Your payslip for <strong>{new DateTime(payroll.Year, payroll.Month, 1):MMMM yyyy}</strong> is now available.</p>
            
            <div class=""salary-box"">
                <p class=""salary-label"">Net Salary</p>
                <div class=""salary-amount"">RM {payroll.NetSalary:N2}</div>
            </div>

            <p>Please find your detailed payslip attached as a PDF document.</p>
            <p>If you have any questions regarding your payslip, please contact the HR department.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email from the HRMS Payroll System.</p>
            <p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            // Send in background with its own scope to prevent 504 timeout
            var scopeFactory = _scopeFactory;
            var toEmail = employee.Email;
            var payrollId = id;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailSvc.SendEmailWithAttachmentAsync(toEmail, subject, body, pdfData, fileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL ERROR] Payslip {payrollId} to {toEmail}: {ex.Message}");
                }
            });

            _logger.LogInformation("Payslip email queued for {Email} for payroll ID {PayrollId}", employee.Email, id);
            return Ok(new { message = $"Payslip email queued for {employee.Email}. It will arrive shortly." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare payslip email for payroll ID {PayrollId}", id);
            return StatusCode(500, new { message = $"Failed to prepare email: {ex.Message}" });
        }
    }

    /// <summary>
    /// GET /api/selfservice/my-attendance - Get current user's attendance records
    /// </summary>
    [HttpGet("my-attendance")]
    public async Task<IActionResult> GetMyAttendance([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        // Default to current month if no dates provided
        var start = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var end = endDate ?? DateTime.Now;

        var attendanceRecords = await _db.Attendances
            .Where(a => a.EmployeeId == employee.Id)
            .Where(a => a.Date >= start && a.Date <= end)
            .OrderByDescending(a => a.Date)
            .Select(a => new
            {
                a.Id,
                a.Date,
                a.Status,
                a.CheckIn,
                a.CheckOut,
                a.WorkHours,
                a.Remarks,
                a.CreatedAt
            })
            .ToListAsync();

        var summary = new
        {
            totalRecords = attendanceRecords.Count,
            presentCount = attendanceRecords.Count(a => a.Status == "Present"),
            absentCount = attendanceRecords.Count(a => a.Status == "Absent"),
            leaveCount = attendanceRecords.Count(a => a.Status == "Leave"),
            halfDayCount = attendanceRecords.Count(a => a.Status == "HalfDay"),
            totalWorkHours = attendanceRecords.Where(a => a.WorkHours > 0).Sum(a => a.WorkHours),
            averageWorkHours = attendanceRecords.Where(a => a.WorkHours > 0).Any() 
                ? attendanceRecords.Where(a => a.WorkHours > 0).Average(a => a.WorkHours) 
                : 0
        };

        return Ok(new
        {
            startDate = start,
            endDate = end,
            summary,
            records = attendanceRecords
        });
    }

    /// <summary>
    /// GET /api/selfservice/my-leave-balance - Get current user's leave balance
    /// </summary>
    [HttpGet("my-leave-balance")]
    public async Task<IActionResult> GetMyLeaveBalance([FromQuery] int? year = null)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var targetYear = year ?? DateTime.Now.Year;

        var leaveBalances = await _db.EmployeeLeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == employee.Id && lb.Year == targetYear)
            .Select(lb => new
            {
                lb.Id,
                lb.LeaveTypeId,
                leaveTypeName = lb.LeaveType.Name,
                leaveTypeCode = lb.LeaveType.Code,
                lb.Year,
                lb.TotalDays,
                lb.UsedDays,
                lb.BalanceDays,
                lb.CarryForwardDays,
                lb.CreatedAt,
                lb.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            employeeId = employee.Id,
            employeeName = employee.Name,
            year = targetYear,
            balances = leaveBalances
        });
    }

    /// <summary>
    /// <summary>
    /// GET /api/selfservice/attendance-periods - Get current user's attendance periods
    /// </summary>
    [HttpGet("attendance-periods")]
    public async Task<IActionResult> GetMyAttendancePeriods([FromQuery] string? status = null)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var query = _db.AttendancePeriods
            .Include(ap => ap.Days)
            .Where(ap => ap.EmployeeId == employee.Id);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(ap => ap.Status == status);

        var attendancePeriods = await query
            .OrderByDescending(ap => ap.StartDate)
            .Select(ap => new
            {
                ap.Id,
                ap.StartDate,
                ap.EndDate,
                ap.Status,
                ap.CreatedAt,
                ap.SubmittedAt,
                ap.ApprovedAt,
                ap.RejectedAt,
                ap.ApprovedBy,
                ap.RejectedBy,
                ap.RejectionReason,
                ap.Remarks,
                periodLabel = ap.StartDate.ToString("MMM dd") + " - " + ap.EndDate.ToString("MMM dd, yyyy"),
                days = ap.Days.OrderBy(d => d.Date).Select(d => new
                {
                    d.Id,
                    d.Date,
                    d.Hours,
                    d.Note,
                    d.Remarks,
                    d.IsPublicHoliday,
                    d.IsWeekend
                })
            })
            .ToListAsync();

        return Ok(attendancePeriods);
    }


    /// <summary>
    /// POST /api/selfservice/attendance-periods - Save (create or update) an attendance period
    /// </summary>
    [HttpPost("attendance-periods")]
    public async Task<IActionResult> SaveAttendancePeriod([FromBody] SaveAttendancePeriodRequest request)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());
        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        // Normalize dates to UTC
        var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var endDate   = DateTime.SpecifyKind(request.EndDate.Date,   DateTimeKind.Utc);

        AttendancePeriod period;

        if (request.Id.HasValue && request.Id.Value > 0)
        {
            // UPDATE existing period
            period = await _db.AttendancePeriods
                .Include(ap => ap.Days)
                .FirstOrDefaultAsync(ap => ap.Id == request.Id.Value && ap.EmployeeId == employee.Id);

            if (period == null)
                return NotFound(new { message = "Attendance period not found" });

            if (period.Status == "Submitted" || period.Status == "Approved")
                return BadRequest(new { message = $"Cannot edit a period with status: {period.Status}" });

            // Remove old days and replace
            _db.RemoveRange(period.Days);
            period.Days.Clear();
        }
        else
        {
            // CREATE new period - check for duplicate
            var existing = await _db.AttendancePeriods
                .FirstOrDefaultAsync(ap => ap.EmployeeId == employee.Id
                                        && ap.StartDate == startDate
                                        && ap.EndDate == endDate);
            if (existing != null)
            {
                // Re-use existing period (treat as update)
                period = await _db.AttendancePeriods
                    .Include(ap => ap.Days)
                    .FirstOrDefaultAsync(ap => ap.Id == existing.Id);

                if (period.Status == "Submitted" || period.Status == "Approved")
                    return BadRequest(new { message = $"Cannot edit a period with status: {period.Status}" });

                _db.RemoveRange(period.Days);
                period.Days.Clear();
            }
            else
            {
                period = new AttendancePeriod
                {
                    EmployeeId = employee.Id,
                    StartDate  = startDate,
                    EndDate    = endDate,
                    Status     = "Draft",
                    CreatedAt  = DateTime.UtcNow
                };
                _db.AttendancePeriods.Add(period);
                await _db.SaveChangesAsync(); // Get period.Id before adding days
            }
        }

        // Add days
        foreach (var d in request.Days)
        {
            period.Days.Add(new AttendancePeriodDay
            {
                AttendancePeriodId = period.Id,
                Date             = DateTime.SpecifyKind(d.Date.Date, DateTimeKind.Utc),
                Hours            = d.Hours,
                Note             = d.Note,
                Remarks          = d.Remarks,
                IsPublicHoliday  = d.IsPublicHoliday,
                IsWeekend        = d.IsWeekend
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Attendance period {Id} saved for employee {EmployeeId}", period.Id, employee.Id);

        return Ok(new
        {
            message = "Attendance period saved successfully",
            id      = period.Id,
            status  = period.Status,
            period.StartDate,
            period.EndDate
        });
    }

    /// <summary>
    /// POST /api/selfservice/attendance-periods/{id}/submit - Submit a saved period for approval
    /// </summary>
    [HttpPost("attendance-periods/{id:int}/submit")]
    public async Task<IActionResult> SubmitAttendancePeriod(int id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());
        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var period = await _db.AttendancePeriods
            .FirstOrDefaultAsync(ap => ap.Id == id && ap.EmployeeId == employee.Id);

        if (period == null)
            return NotFound(new { message = "Attendance period not found or does not belong to you" });

        if (period.Status == "Submitted")
            return BadRequest(new { message = "Attendance period is already submitted" });

        if (period.Status == "Approved")
            return BadRequest(new { message = "Attendance period is already approved" });

        period.Status      = "Submitted";
        period.SubmittedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Attendance period {Id} submitted by employee {EmployeeId}", id, employee.Id);

        return Ok(new
        {
            message     = "Attendance period submitted successfully for approval",
            id          = period.Id,
            status      = period.Status,
            submittedAt = period.SubmittedAt
        });
    }

    /// <summary>
    /// GET /api/selfservice/my-leave-requests - Get current user's leave requests
    /// </summary>
    [HttpGet("my-leave-requests")]
    public async Task<IActionResult> GetMyLeaveRequests([FromQuery] string? status = null)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var query = _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == employee.Id);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(lr => lr.Status == status);

        var leaveRequests = await query
            .OrderByDescending(lr => lr.RequestedOn)
            .Select(lr => new
            {
                lr.Id,
                lr.LeaveTypeId,
                leaveTypeName = lr.LeaveType.Name,
                lr.StartDate,
                lr.EndDate,
                lr.TotalDays,
                lr.Reason,
                lr.Status,
                lr.RequestedOn,
                lr.ApprovedBy,
                lr.ApprovedOn,
                lr.ApprovalRemarks,
                lr.CancelledOn,
                lr.CancellationReason
            })
            .ToListAsync();

        return Ok(leaveRequests);
    }

    /// <summary>
    /// GET /api/selfservice/my-timesheets - Get current user's timesheets
    /// </summary>
    [HttpGet("my-timesheets")]
    public async Task<IActionResult> GetMyTimesheets([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var query = _db.Timesheets
            .Where(t => t.EmployeeId == employee.Id);

        if (year.HasValue)
            query = query.Where(t => t.Year == year.Value);

        if (month.HasValue)
            query = query.Where(t => t.Month == month.Value);

        var timesheets = await query
            .OrderByDescending(t => t.Year)
            .ThenByDescending(t => t.Month)
            .Select(t => new
            {
                t.Id,
                t.Month,
                t.Year,
                monthName = new DateTime(t.Year, t.Month, 1).ToString("MMMM yyyy"),
                t.TotalWorkingDays,
                t.TotalPresent,
                t.TotalMedicalLeave,
                t.TotalAbsent,
                t.TotalLeave,
                t.TotalHalfDay,
                t.TotalPublicHolidays,
                t.TotalWorkHours,
                t.Status,
                t.GeneratedOn,
                t.ApprovedBy,
                t.ApprovedOn,
                t.Remarks
            })
            .ToListAsync();

        return Ok(timesheets);
    }

    /// <summary>
    /// POST /api/selfservice/submit-timesheet/{id} - Submit timesheet for approval
    /// </summary>
    [HttpPost("submit-timesheet/{id:int}")]
    public async Task<IActionResult> SubmitTimesheet(int id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        var timesheet = await _db.Timesheets
            .FirstOrDefaultAsync(t => t.Id == id && t.EmployeeId == employee.Id);

        if (timesheet == null)
            return NotFound(new { message = "Timesheet not found or does not belong to you" });

        if (timesheet.Status != "Draft")
            return BadRequest(new { message = $"Only Draft timesheets can be submitted. Current status: {timesheet.Status}" });

        timesheet.Status = "Submitted";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Timesheet {TimesheetId} submitted by employee {EmployeeId}", id, employee.Id);

        return Ok(new
        {
            message = "Timesheet submitted successfully! It is now pending approval.",
            timesheet = new
            {
                timesheet.Id,
                timesheet.Month,
                timesheet.Year,
                timesheet.Status,
                monthName = new DateTime(timesheet.Year, timesheet.Month, 1).ToString("MMMM yyyy")
            }
        });
    }

    /// <summary>
    /// POST /api/selfservice/change-password - Change current user's password
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Verify old password
        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect" });

        // Validate new password
        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters long" });

        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new { message = "New password and confirmation do not match" });

        // Hash and update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully for user: {Username}", username);

        return Ok(new { message = "Password changed successfully" });
    }


    [HttpGet("timesheet-template")]
    public async Task<IActionResult> DownloadTimesheetTemplate([FromQuery] int month, [FromQuery] int year)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var holidays = await _db.PublicHolidays
                .Where(h => h.Year == year && h.Date >= startDate && h.Date <= endDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Timesheet");

            // HEADER
            ws.Cell(2, 1).Value = "PERSONNEL TIME SHEET / DAILY ACTIVITIES REPORT";
            ws.Range(2, 1, 2, 10).Merge();
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // EMPLOYEE INFO
            ws.Cell(3, 1).Value = "EMPLOYEE:";
            ws.Cell(3, 2).Value = employee.Name;
            ws.Range(3, 2, 3, 4).Merge();
            ws.Cell(3, 2).Style.Font.Bold = true;

            ws.Cell(3, 6).Value = "STATUS:";
            ws.Cell(3, 7).Value = "Draft";
            ws.Range(3, 7, 3, 8).Merge();

            ws.Cell(3, 9).Value = $"{startDate:dd-MMM-yyyy} to {endDate:dd-MMM-yyyy}";
            ws.Range(3, 9, 3, 10).Merge();
            ws.Cell(3, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(4, 1).Value = "DESIGNATION:";
            ws.Cell(4, 2).Value = employee.Designation;
            ws.Range(4, 2, 4, 6).Merge();

            // INSTRUCTIONS
            ws.Cell(5, 1).Value = "INSTRUCTIONS: Column C (1-12) = AM hours, Column H (1-12) = PM hours (1=1PM, 6=6PM). Example: 9 to 6 = 9AM to 6PM";
            ws.Range(5, 1, 5, 10).Merge();
            ws.Cell(5, 1).Style.Font.Italic = true;
            ws.Cell(5, 1).Style.Font.FontColor = XLColor.DarkBlue;
            ws.Cell(5, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;

            // COLUMN HEADERS
            int headerRow = 7;
            ws.Cell(headerRow, 1).Value = "DAY";
            ws.Cell(headerRow, 2).Value = "DATE";
            ws.Cell(headerRow, 3).Value = "TIME IN\n(1-12 AM)";
            ws.Cell(headerRow, 4).Value = "ICH: TIME IN";
            ws.Cell(headerRow, 5).Value = "CH: TIME IN";
            ws.Cell(headerRow, 6).Value = "ICH: TIME OUT";
            ws.Cell(headerRow, 7).Value = "CH: TIME OUT";
            ws.Cell(headerRow, 8).Value = "TIME OUT\n(1-12 PM)";
            ws.Cell(headerRow, 9).Value = "TOTAL HRS";
            ws.Cell(headerRow, 10).Value = "OVERTIME";

            var headerRange = ws.Range(headerRow, 1, headerRow, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Alignment.WrapText = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Row(headerRow).Height = 30;

            // EXAMPLE ROW
            int exampleRow = headerRow + 1;
            ws.Cell(exampleRow, 1).Value = "EXAMPLE";
            ws.Cell(exampleRow, 2).Value = "01/01/2026";
            ws.Cell(exampleRow, 3).Value = "9";
            ws.Cell(exampleRow, 4).Value = "//////";
            ws.Cell(exampleRow, 5).Value = "//////";
            ws.Cell(exampleRow, 6).Value = "//////";
            ws.Cell(exampleRow, 7).Value = "//////";
            ws.Cell(exampleRow, 8).Value = "6";
            ws.Cell(exampleRow, 9).Value = "8";
            ws.Cell(exampleRow, 10).Value = "0";

            var exampleRange = ws.Range(exampleRow, 1, exampleRow, 10);
            exampleRange.Style.Fill.BackgroundColor = XLColor.Yellow;
            exampleRange.Style.Font.Bold = true;
            exampleRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Add helper note below example
            ws.Cell(exampleRow, 11).Value = "? 9 AM to 6 PM (6 = 18:00)";
            ws.Cell(exampleRow, 11).Style.Font.Italic = true;
            ws.Cell(exampleRow, 11).Style.Font.FontColor = XLColor.DarkGreen;

            // DAILY ATTENDANCE DATA
            int dataStartRow = exampleRow + 1;
            int currentRow = dataStartRow;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek;
                var dayName = date.ToString("dddd");
                var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
                var holiday = holidays.FirstOrDefault(h => h.Date.Date == date.Date);

                ws.Cell(currentRow, 1).Value = dayName;
                ws.Cell(currentRow, 2).Value = date.ToString("dd/MM/yyyy");

                if (isWeekend)
                {
                    ws.Cell(currentRow, 3).Value = "WEEKEND";
                    ws.Range(currentRow, 3, currentRow, 8).Merge();
                    ws.Cell(currentRow, 3).Style.Font.Bold = true;
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range(currentRow, 1, currentRow, 10).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    ws.Cell(currentRow, 9).Value = 0;
                    ws.Cell(currentRow, 10).Value = 0;
                }
                else if (holiday != null)
                {
                    ws.Cell(currentRow, 3).Value = $"Public Holiday - {holiday.Name}";
                    ws.Range(currentRow, 3, currentRow, 8).Merge();
                    ws.Cell(currentRow, 3).Style.Font.Bold = true;
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Range(currentRow, 1, currentRow, 10).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    ws.Cell(currentRow, 9).Value = 0;
                    ws.Cell(currentRow, 10).Value = 0;
                }
                else
                {
                    // Empty for employee to fill
                    ws.Cell(currentRow, 3).Value = "";
                    ws.Cell(currentRow, 4).Value = "//////";
                    ws.Cell(currentRow, 5).Value = "//////";
                    ws.Cell(currentRow, 6).Value = "//////";
                    ws.Cell(currentRow, 7).Value = "//////";
                    ws.Cell(currentRow, 8).Value = "";
                    ws.Cell(currentRow, 9).Value = "";
                    ws.Cell(currentRow, 10).Value = 0;
                }

                ws.Range(currentRow, 1, currentRow, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(currentRow, 1, currentRow, 10).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                currentRow++;
            }

            // TOTALS ROW
            int totalsRow = currentRow;
            ws.Cell(totalsRow, 1).Value = "Totals:";
            ws.Cell(totalsRow, 1).Style.Font.Bold = true;
            ws.Cell(totalsRow, 9).Value = "";
            ws.Cell(totalsRow, 9).Style.Font.Bold = true;
            ws.Cell(totalsRow, 9).Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell(totalsRow, 10).Value = 0;
            ws.Range(totalsRow, 1, totalsRow, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            // SIGNATURE SECTION
            int sigRow = totalsRow + 3;

            ws.Cell(sigRow, 1).Value = "SUBMITTED BY";
            ws.Range(sigRow, 1, sigRow, 3).Merge();
            ws.Cell(sigRow, 1).Style.Font.Bold = true;
            ws.Cell(sigRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(sigRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(sigRow, 4).Value = "VERIFIED BY";
            ws.Range(sigRow, 4, sigRow, 6).Merge();
            ws.Cell(sigRow, 4).Style.Font.Bold = true;
            ws.Cell(sigRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(sigRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(sigRow, 7).Value = "APPROVED BY";
            ws.Range(sigRow, 7, sigRow, 10).Merge();
            ws.Cell(sigRow, 7).Style.Font.Bold = true;
            ws.Cell(sigRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(sigRow, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            int detailsRow = sigRow + 2;
            ws.Cell(detailsRow, 1).Value = "NAME: " + employee.Name;
            ws.Range(detailsRow, 1, detailsRow, 3).Merge();
            ws.Cell(detailsRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(detailsRow, 4).Value = "NAME:";
            ws.Range(detailsRow, 4, detailsRow, 6).Merge();
            ws.Cell(detailsRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(detailsRow, 7).Value = "NAME:";
            ws.Range(detailsRow, 7, detailsRow, 10).Merge();
            ws.Cell(detailsRow, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            int signRow = detailsRow + 2;
            ws.Cell(signRow, 1).Value = "SIGN:";
            ws.Range(signRow, 1, signRow, 3).Merge();
            ws.Cell(signRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(signRow, 4).Value = "SIGN:";
            ws.Range(signRow, 4, signRow, 6).Merge();
            ws.Cell(signRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(signRow, 7).Value = "SIGN:";
            ws.Range(signRow, 7, signRow, 10).Merge();
            ws.Cell(signRow, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            int dateRow = signRow + 2;
            ws.Cell(dateRow, 1).Value = "DATE:";
            ws.Range(dateRow, 1, dateRow, 3).Merge();
            ws.Cell(dateRow, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(dateRow, 4).Value = "DATE:";
            ws.Range(dateRow, 4, dateRow, 6).Merge();
            ws.Cell(dateRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(dateRow, 7).Value = "DATE:";
            ws.Range(dateRow, 7, dateRow, 10).Merge();
            ws.Cell(dateRow, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Auto-fit columns
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var fileName = $"Timesheet_Template_{year}_{month:D2}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating timesheet template");
            return StatusCode(500, new { message = "Failed to generate template: " + ex.Message });
        }
    }


    [HttpPost("upload-timesheet")]
    public async Task<IActionResult> UploadTimesheet([FromForm] IFormFile file, [FromForm] int month, [FromForm] int year)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        // Find user account to get their email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());

        if (employee == null)
            return NotFound(new { message = "Employee profile not found" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            return BadRequest(new { message = "Invalid file format. Please upload an Excel file (.xlsx or .xls)" });

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            int totalPresent = 0;      // Regular working days with hours filled
            int totalMedicalLeave = 0; // MC count
            int totalAbsent = 0;       // AL count (Annual Leave)
            int totalLeave = 0;        // EL count (Emergency Leave)
            int totalHalfDay = 0;
            decimal totalWorkHours = 0;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Read data starting from row 9 (after example row)
            // Row 7 = Headers, Row 8 = Example, Row 9+ = Data
            int currentRow = 7; // Start from row 7 to catch all data
            var attendanceRecordsToCreate = new List<Attendance>();
            decimal leaveDaysCount = 0;

            // Loop through each day and create PENDING attendance records
            while (!worksheet.Cell(currentRow, 1).IsEmpty() && currentRow < 100)
            {
                var dayName = worksheet.Cell(currentRow, 1).GetString();
                var dateStr = worksheet.Cell(currentRow, 2).GetString();
                var timeInStr = worksheet.Cell(currentRow, 3).GetString();  // Column C: TIME IN
                var timeOutStr = worksheet.Cell(currentRow, 6).GetString(); // Column F: TIME OUT (final)
                var workHoursStr = worksheet.Cell(currentRow, 7).GetString(); // Column G: HOURS WORKED

                _logger.LogInformation($"Row {currentRow}: Day={dayName}, Date={dateStr}, TimeIn={timeInStr}, TimeOut={timeOutStr}, Hours={workHoursStr}");

                // Parse date

                // Skip EXAMPLE row
                if (dayName.ToUpper().Contains("EXAMPLE"))
                {
                    _logger.LogInformation($"Row {currentRow}: Skipping EXAMPLE row");
                    currentRow++;
                    continue;
                }

                // Parse date
                DateTime attendanceDate;
                if (!DateTime.TryParse(dateStr, out attendanceDate))
                {
                    _logger.LogWarning($"Row {currentRow}: Failed to parse date '{dateStr}'");
                    currentRow++;
                    continue;
                }
                // Ensure UTC Kind so Npgsql accepts it for 'timestamp with time zone'
                attendanceDate = DateTime.SpecifyKind(attendanceDate.Date, DateTimeKind.Utc);

                // Check if it's a weekend or holiday
                if (timeInStr.Contains("WEEKEND") || timeInStr.Contains("Public Holiday"))
                {
                    _logger.LogInformation($"Row {currentRow}: Skipping weekend/holiday");
                    currentRow++;
                    continue;
                }

                // Try to parse work hours
                decimal workHours = 0;
                bool hasWorkHours = decimal.TryParse(workHoursStr, out workHours) && workHours > 0;

                // Try to parse time IN as TimeSpan (for times like "9:00:00")
                TimeSpan timeInSpan;
                bool hasTimeIn = TimeSpan.TryParse(timeInStr, out timeInSpan);

                // Try to parse time OUT as TimeSpan (for times like "18:00:00")
                TimeSpan timeOutSpan;
                bool hasTimeOut = TimeSpan.TryParse(timeOutStr, out timeOutSpan);

                string status = "";
                DateTime? checkInTime = null;
                DateTime? checkOutTime = null;

                // Priority 1: Check for Medical Leave (MC or ML)
                if (!string.IsNullOrWhiteSpace(timeInStr) &&
                    (timeInStr.ToUpper().Contains("MC") || timeInStr.ToUpper().Contains("ML") || timeInStr.ToUpper().Contains("MEDICAL")))
                {
                    totalMedicalLeave++;
                    leaveDaysCount++;
                    status = "Leave"; // Store as "Leave" in attendance table
                    _logger.LogInformation($"Row {currentRow}: Detected Medical Leave (MC) - totalMedicalLeave={totalMedicalLeave}");
                }
                // Priority 2: Check for Annual Leave (AL)
                else if (!string.IsNullOrWhiteSpace(timeInStr) && timeInStr.ToUpper().Contains("AL"))
                {
                    totalAbsent++;
                    leaveDaysCount++;
                    status = "Absent";
                    _logger.LogInformation($"Row {currentRow}: Detected Annual Leave (AL) - totalAbsent={totalAbsent}");
                }
                // Priority 3: Check for Emergency Leave (EL)
                else if (!string.IsNullOrWhiteSpace(timeInStr) && timeInStr.ToUpper().Contains("EL"))
                {
                    totalLeave++;
                    leaveDaysCount++;
                    status = "Leave";
                    _logger.LogInformation($"Row {currentRow}: Detected Emergency Leave (EL) - totalLeave={totalLeave}");
                }
                // Priority 4: Check for Half Day
                else if (!string.IsNullOrWhiteSpace(timeInStr) && timeInStr.ToUpper().Contains("HALF"))
                {
                    totalHalfDay++;
                    leaveDaysCount += 0.5m;
                    if (hasWorkHours)
                    {
                        totalWorkHours += workHours;
                    }
                    status = "HalfDay";
                    _logger.LogInformation($"Row {currentRow}: Detected Half Day - totalHalfDay={totalHalfDay}");
                }
                // Priority 5: Check for Present (has valid times and work hours)
                else if (hasTimeIn && hasTimeOut && hasWorkHours)
                {
                    totalPresent++;
                    totalWorkHours += workHours;
                    status = "Present";

                    // Convert TimeSpan to DateTime (UTC Kind required by Npgsql)
                    checkInTime = DateTime.SpecifyKind(attendanceDate.Date.Add(timeInSpan), DateTimeKind.Utc);
                    checkOutTime = DateTime.SpecifyKind(attendanceDate.Date.Add(timeOutSpan), DateTimeKind.Utc);
                    
                    _logger.LogInformation($"Row {currentRow}: Detected Present - totalPresent={totalPresent}, hours={workHours}, totalWorkHours={totalWorkHours}");
                }
                else
                {
                    _logger.LogWarning($"Row {currentRow}: No status matched - hasTimeIn={hasTimeIn}, hasTimeOut={hasTimeOut}, hasWorkHours={hasWorkHours}");
                }

                // Create PENDING attendance record (will be confirmed on approval)
                if (!string.IsNullOrEmpty(status))
                {
                    attendanceRecordsToCreate.Add(new Attendance
                    {
                        EmployeeId = employee.Id,
                        Date = attendanceDate,
                        Status = status,
                        CheckIn = checkInTime,
                        CheckOut = checkOutTime,
                        WorkHours = workHours,
                        Remarks = $"PENDING APPROVAL - Uploaded from timesheet {DateTime.UtcNow:yyyy-MM-dd}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                currentRow++;
            }

            // Delete old pending attendance records for this month before inserting new ones
            var existingPendingAttendance = await _db.Attendances
                .Where(a => a.EmployeeId == employee.Id 
                    && a.Date >= startDate 
                    && a.Date <= endDate
                    && a.Remarks != null 
                    && a.Remarks.Contains("PENDING APPROVAL"))
                .ToListAsync();
            
            if (existingPendingAttendance.Any())
            {
                _db.Attendances.RemoveRange(existingPendingAttendance);
            }

            // Insert new pending attendance records
            if (attendanceRecordsToCreate.Any())
            {
                await _db.Attendances.AddRangeAsync(attendanceRecordsToCreate);
            }

            // Calculate working days
            var publicHolidays = await _db.PublicHolidays
                .Where(h => h.Year == year && h.Date >= startDate && h.Date <= endDate)
                .ToListAsync();

            // Calculate working days inline (excluding weekends and public holidays)
            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    if (!publicHolidays.Any(h => h.Date.Date == date.Date))
                    {
                        workingDays++;
                    }
                }
            }

            // Create or update timesheet
            var existingTimesheet = await _db.Timesheets
                .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Month == month && t.Year == year);

            if (existingTimesheet != null)
            {
                // Update existing
                existingTimesheet.TotalWorkingDays = workingDays;
                existingTimesheet.TotalPresent = totalPresent;
                existingTimesheet.TotalMedicalLeave = totalMedicalLeave;
                existingTimesheet.TotalAbsent = totalAbsent;
                existingTimesheet.TotalLeave = totalLeave;
                existingTimesheet.TotalHalfDay = totalHalfDay;
                existingTimesheet.TotalPublicHolidays = publicHolidays.Count;
                existingTimesheet.TotalWorkHours = totalWorkHours;
                existingTimesheet.Status = "Draft";
                existingTimesheet.GeneratedOn = DateTime.UtcNow;
            }
            else
            {
                // Create new
                var timesheet = new Timesheet
                {
                    EmployeeId = employee.Id,
                    Month = month,
                    Year = year,
                    TotalWorkingDays = workingDays,
                    TotalPresent = totalPresent,
                    TotalMedicalLeave = totalMedicalLeave,
                    TotalAbsent = totalAbsent,
                    TotalLeave = totalLeave,
                    TotalHalfDay = totalHalfDay,
                    TotalPublicHolidays = publicHolidays.Count,
                    TotalWorkHours = totalWorkHours,
                    Status = "Draft",
                    GeneratedOn = DateTime.UtcNow
                };

                _db.Timesheets.Add(timesheet);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Timesheet uploaded successfully! Attendance records pending approval. Leave balance will be deducted after admin approval.",
                summary = new
                {
                    workingDays,
                    totalPresent,
                    totalMedicalLeave,
                    totalAbsent,
                    totalLeave,
                    totalHalfDay,
                    totalWorkHours,
                    publicHolidays = publicHolidays.Count,
                    pendingAttendanceRecords = attendanceRecordsToCreate.Count,
                    pendingLeaveDeduction = leaveDaysCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing timesheet upload for employee {EmployeeId}", employee?.Id);
            return StatusCode(500, new { message = "Failed to process timesheet: " + ex.Message });
        }
    }
}

// Request models
public class UpdateSelfProfileRequest
{
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? BankId { get; set; }
    public string? AccountNumber { get; set; }
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}



