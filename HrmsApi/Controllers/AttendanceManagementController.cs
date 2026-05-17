using HrmsApi.Data;
using HrmsApi.Models;
using HrmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")] // Only admins and managers can approve/reject
public class AttendanceManagementController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<AttendanceManagementController> _logger;
    private readonly IEmailQueue _emailQueue;

    public AttendanceManagementController(HrmsDbContext db, IEmailService emailService, ILogger<AttendanceManagementController> logger, IEmailQueue emailQueue)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _emailQueue = emailQueue;
    }

    /// <summary>
    /// GET /api/attendancemanagement/pending - Get all pending (submitted) attendance periods for review
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingPeriods()
    {
        var periods = await _db.AttendancePeriods
            .Include(p => p.Employee)
            .Include(p => p.Days)
            .Where(p => p.Status == "Submitted")
            .OrderBy(p => p.SubmittedAt)
            .Select(p => new
            {
                p.Id,
                p.EmployeeId,
                employeeName = p.Employee.Name,
                employeeEmail = p.Employee.Email,
                p.StartDate,
                p.EndDate,
                p.Status,
                p.SubmittedAt,
                p.Remarks,
                // Exclude leave days from total hours (days with a note have 0 working hours)
                totalHours = p.Days.Where(d => string.IsNullOrEmpty(d.Note)).Sum(d => d.Hours),
                leaveCount = p.Days.Count(d => !string.IsNullOrEmpty(d.Note)),
                alCount = p.Days.Count(d => d.Note == "AL"),
                elCount = p.Days.Count(d => d.Note == "EL"),
                mcCount = p.Days.Count(d => d.Note == "MC"),
                dayCount = p.Days.Count
            })
            .ToListAsync();

        return Ok(periods);
    }

    /// <summary>
    /// GET /api/attendancemanagement/all - Get all attendance periods (all statuses)
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllPeriods([FromQuery] string? status = null, [FromQuery] int? employeeId = null)
    {
        var query = _db.AttendancePeriods
            .Include(p => p.Employee)
            .Include(p => p.Days)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);

        var periods = await query
            .OrderByDescending(p => p.StartDate)
            .Select(p => new
            {
                p.Id,
                p.EmployeeId,
                employeeName = p.Employee.Name,
                employeeEmail = p.Employee.Email,
                p.StartDate,
                p.EndDate,
                p.Status,
                p.CreatedAt,
                p.SubmittedAt,
                p.ApprovedAt,
                p.RejectedAt,
                p.RejectionReason,
                p.Remarks,
                totalHours = p.Days.Where(d => string.IsNullOrEmpty(d.Note)).Sum(d => d.Hours),
                leaveCount = p.Days.Count(d => !string.IsNullOrEmpty(d.Note)),
                alCount = p.Days.Count(d => d.Note == "AL"),
                elCount = p.Days.Count(d => d.Note == "EL"),
                mcCount = p.Days.Count(d => d.Note == "MC")
            })
            .ToListAsync();

        return Ok(periods);
    }

    /// <summary>
    /// GET /api/attendancemanagement/{id}/details - Get detailed view of an attendance period
    /// Includes approved leave requests that overlap with the period dates
    /// </summary>
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetPeriodDetails(int id)
    {
        var period = await _db.AttendancePeriods
            .Include(p => p.Employee)
            .Include(p => p.Days)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();

        if (period == null)
            return NotFound(new { message = "Attendance period not found" });

        // Get approved leave requests that overlap with this period
        var approvedLeaves = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == period.EmployeeId 
                      && lr.Status == "Approved"
                      && lr.StartDate <= period.EndDate
                      && lr.EndDate >= period.StartDate)
            .Select(lr => new
            {
                lr.Id,
                leaveType = lr.LeaveType.Name,
                leaveTypeCode = lr.LeaveType.Code,
                lr.StartDate,
                lr.EndDate,
                lr.TotalDays,
                lr.Reason,
                lr.ApprovedOn,
                lr.ApprovalRemarks,
                lr.Status
            })
            .OrderBy(lr => lr.StartDate)
            .ToListAsync();

        // Get pending leave requests that overlap with this period
        var pendingLeaves = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == period.EmployeeId 
                      && lr.Status == "Pending"
                      && lr.StartDate <= period.EndDate
                      && lr.EndDate >= period.StartDate)
            .Select(lr => new
            {
                lr.Id,
                leaveType = lr.LeaveType.Name,
                leaveTypeCode = lr.LeaveType.Code,
                lr.StartDate,
                lr.EndDate,
                lr.TotalDays,
                lr.Reason,
                lr.RequestedOn,
                lr.Status
            })
            .OrderBy(lr => lr.StartDate)
            .ToListAsync();

        var result = new
        {
            period.Id,
            period.EmployeeId,
            employeeName = period.Employee.Name,
            employeeEmail = period.Employee.Email,
            employeeDepartment = period.Employee.Department != null ? period.Employee.Department.Name : "",
            period.StartDate,
            period.EndDate,
            period.Status,
            period.CreatedAt,
            period.SubmittedAt,
            period.ApprovedAt,
            period.RejectedAt,
            period.ApprovedBy,
            period.RejectedBy,
            period.RejectionReason,
            period.Remarks,
            days = period.Days.OrderBy(d => d.Date).Select(d => new
            {
                d.Id,
                d.Date,
                d.Hours,
                d.Note,
                d.Remarks,
                d.IsPublicHoliday,
                d.IsWeekend
            }).ToList(),
            summary = new
            {
                totalHours = period.Days.Sum(d => d.Hours),
                workingDays = period.Days.Count(d => d.Hours > 0),
                alCount = period.Days.Count(d => d.Note == "AL"),
                elCount = period.Days.Count(d => d.Note == "EL"),
                mcCount = period.Days.Count(d => d.Note == "MC")
            },
            approvedLeaves = approvedLeaves,
            pendingLeaves = pendingLeaves,
            hasPendingLeaves = pendingLeaves.Count > 0
        };

        return Ok(result);
    }

    /// <summary>
    /// POST /api/attendancemanagement/{id}/approve - Approve an attendance period
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApprovePeriod(int id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (adminUser == null)
            return NotFound(new { message = "Admin user not found" });

        var period = await _db.AttendancePeriods
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (period == null)
            return NotFound(new { message = "Attendance period not found" });

        if (period.Status != "Submitted")
            return BadRequest(new { message = $"Cannot approve a period with status: {period.Status}. Only Submitted periods can be approved." });

        // Check for pending leave requests
        var hasPendingLeaves = await _db.LeaveRequests
            .AnyAsync(lr => lr.EmployeeId == period.EmployeeId
                         && lr.Status == "Pending"
                         && lr.StartDate <= period.EndDate
                         && lr.EndDate >= period.StartDate);

        if (hasPendingLeaves)
            return BadRequest(new { message = "Cannot approve attendance. There are pending leave requests for this period. Please approve or reject leave requests first." });

        // Update status
        period.Status = "Approved";
        period.ApprovedAt = DateTime.UtcNow;
        period.ApprovedBy = adminUser.Id;

        // Deduct leave balances based on day notes (AL/EL/MC)
        var days = await _db.AttendancePeriodDays
            .Where(d => d.AttendancePeriodId == id && !string.IsNullOrEmpty(d.Note))
            .ToListAsync();

        if (days.Any())
        {
            var year = period.StartDate.Year;

            // Group leave days by note type and deduct from corresponding leave balance
            var leaveGroups = days
                .Where(d => d.Note == "AL" || d.Note == "EL" || d.Note == "MC")
                .GroupBy(d => d.Note);

            foreach (var group in leaveGroups)
            {
                var leaveCode = group.Key; // AL, EL, MC
                var daysCount = group.Count();

                var leaveType = await _db.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.Code == leaveCode && lt.IsActive);

                if (leaveType == null) continue;

                var balance = await _db.EmployeeLeaveBalances
                    .FirstOrDefaultAsync(b => b.EmployeeId == period.EmployeeId
                                           && b.LeaveTypeId == leaveType.Id
                                           && b.Year == year);

                if (balance != null)
                {
                    balance.UsedDays += daysCount;
                    balance.BalanceDays = balance.TotalDays + balance.CarryForwardDays - balance.UsedDays;
                    balance.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("Deducted {Days} {Code} days from employee {EmployeeId} balance for year {Year}",
                        daysCount, leaveCode, period.EmployeeId, year);
                }
            }
        }

        await _db.SaveChangesAsync();

        // Queue email — returns instantly, no 504 risk
        var emailSent = false;
        try
        {
            _emailQueue.Enqueue(new EmailJob
            {
                ToEmail = period.Employee.Email,
                Subject = $"Attendance Period Approved - {period.StartDate:dd MMM yyyy} to {period.EndDate:dd MMM yyyy}",
                Body    = AttendanceEmailBodies.BuildAttendanceApprovedBody(period.Employee.Name, period.StartDate, period.EndDate)
            });
            emailSent = true;
            _logger.LogInformation("Attendance approved email queued for {Email}, period {PeriodId}", period.Employee.Email, period.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue attendance approved email for {Email}", period.Employee.Email);
        }

        return Ok(new
        {
            message = "Attendance period approved successfully",
            periodId = period.Id,
            status = period.Status,
            approvedAt = period.ApprovedAt,
            emailSent
        });
    }

    /// <summary>
    /// POST /api/attendancemanagement/{id}/reject - Reject an attendance period with reason
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectPeriod(int id, [FromBody] RejectAttendanceRequest request)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (adminUser == null)
            return NotFound(new { message = "Admin user not found" });

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
            return BadRequest(new { message = "Rejection reason is required" });

        var period = await _db.AttendancePeriods
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (period == null)
            return NotFound(new { message = "Attendance period not found" });

        if (period.Status != "Submitted")
            return BadRequest(new { message = $"Cannot reject a period with status: {period.Status}. Only Submitted periods can be rejected." });

        // Update status
        period.Status = "Rejected";
        period.RejectedAt = DateTime.UtcNow;
        period.RejectedBy = adminUser.Id;
        period.RejectionReason = request.RejectionReason;

        await _db.SaveChangesAsync();

        // Queue email — returns instantly, no 504 risk
        var emailSent = false;
        try
        {
            _emailQueue.Enqueue(new EmailJob
            {
                ToEmail = period.Employee.Email,
                Subject = $"Attendance Period Rejected - {period.StartDate:dd MMM yyyy} to {period.EndDate:dd MMM yyyy}",
                Body    = AttendanceEmailBodies.BuildAttendanceRejectedBody(period.Employee.Name, period.StartDate, period.EndDate, request.RejectionReason)
            });
            emailSent = true;
            _logger.LogInformation("Attendance rejected email queued for {Email}, period {PeriodId}", period.Employee.Email, period.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue attendance rejected email for {Email}", period.Employee.Email);
        }

        return Ok(new
        {
            message = "Attendance period rejected successfully",
            periodId = period.Id,
            status = period.Status,
            rejectedAt = period.RejectedAt,
            rejectionReason = period.RejectionReason,
            emailSent
        });
    }

    /// <summary>
    /// POST /api/attendancemanagement/{id}/notify-missing-leaves
    /// Sends an email to the employee listing days with 0 hours and no leave request
    /// </summary>
    [HttpPost("{id}/notify-missing-leaves")]
    public async Task<IActionResult> NotifyMissingLeaves(int id)
    {
        var period = await _db.AttendancePeriods
            .Include(p => p.Employee)
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (period == null)
            return NotFound(new { message = "Attendance period not found" });

        // Find 0-hour working days (not weekend, not public holiday) with no leave note
        var missingDays = period.Days
            .Where(d => d.Hours == 0 && !d.IsWeekend && !d.IsPublicHoliday && string.IsNullOrEmpty(d.Note))
            .OrderBy(d => d.Date)
            .ToList();

        if (!missingDays.Any())
            return BadRequest(new { message = "No missing leave days found for this period" });

        var daysList = string.Join("", missingDays.Select(d =>
            $"<li style='padding:4px 0'><strong>{d.Date:dd MMM yyyy (ddd)}</strong> — 0 hours, no leave type recorded</li>"));

        var body = $@"<!DOCTYPE html><html><head><style>
          body{{font-family:Arial,sans-serif;color:#333;}}
          .header{{background:linear-gradient(135deg,#f59e0b,#d97706);color:white;padding:26px;text-align:center;border-radius:8px 8px 0 0;}}
          .header h1{{margin:0;font-size:20px;}} .body{{background:#f8f9fa;padding:24px;border-radius:0 0 8px 8px;}}
          ul{{background:white;border:1px solid #e2e8f0;border-radius:8px;padding:16px 16px 16px 32px;margin:14px 0;}}
          .notice{{background:#fffbeb;border:1px solid #fcd34d;color:#92400e;padding:12px 14px;border-radius:8px;font-size:13px;margin-top:14px;}}
          .footer{{text-align:center;font-size:12px;color:#888;margin-top:18px;}}
        </style></head><body>
          <div class=""header""><h1>⚠️ Action Required: Missing Leave Request</h1></div>
          <div class=""body"">
            <p>Dear <strong>{period.Employee.Name}</strong>,</p>
            <p>Your attendance submission for <strong>{period.StartDate:dd MMM yyyy} – {period.EndDate:dd MMM yyyy}</strong> has <strong>{missingDays.Count} day(s)</strong> with 0 hours and no leave type recorded.</p>
            <p><strong>Days requiring a leave request:</strong></p>
            <ul>{daysList}</ul>
            <div class=""notice"">
              📋 Please log in to <strong>Employee Self-Service → My Leaves</strong> and submit a leave request for the above dates before your attendance can be approved.
            </div>
          </div>
          <div class=""footer"">© {DateTime.Now.Year} HRMS. This is an automated notification.</div>
        </body></html>";

        _emailQueue.Enqueue(new EmailJob
        {
            ToEmail = period.Employee.Email,
            Subject = $"Action Required: Missing Leave Request — {period.StartDate:dd MMM} to {period.EndDate:dd MMM yyyy}",
            Body    = body
        });

        _logger.LogInformation("Missing leave notification sent to {Email} for period {Id}", period.Employee.Email, id);
        return Ok(new { message = $"Notification sent to {period.Employee.Email}", missingDays = missingDays.Count });
    }

    /// <summary>
    /// POST /api/attendancemanagement/{id}/create-leave-on-behalf
    /// Admin creates and auto-approves a leave request for 0-hour days on behalf of employee
    /// </summary>
    [HttpPost("{id}/create-leave-on-behalf")]
    public async Task<IActionResult> CreateLeaveOnBehalf(int id, [FromBody] CreateLeaveOnBehalfRequest request)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (adminUser == null) return Unauthorized();

        var period = await _db.AttendancePeriods
            .Include(p => p.Employee)
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (period == null)
            return NotFound(new { message = "Attendance period not found" });

        var leaveType = await _db.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Code == request.LeaveTypeCode && lt.IsActive);
        if (leaveType == null)
            return BadRequest(new { message = $"Leave type '{request.LeaveTypeCode}' not found" });

        var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var endDate   = DateTime.SpecifyKind(request.EndDate.Date,   DateTimeKind.Utc);

        // Check if leave request already exists for this range
        var existing = await _db.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.EmployeeId == period.EmployeeId
                                    && lr.StartDate == startDate
                                    && lr.EndDate == endDate
                                    && lr.LeaveTypeId == leaveType.Id);
        if (existing != null)
            return BadRequest(new { message = "A leave request already exists for these dates" });

        // Count working days in range
        decimal totalDays = 0;
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            var dayName = d.DayOfWeek;
            if (dayName != DayOfWeek.Saturday && dayName != DayOfWeek.Sunday)
                totalDays++;
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId       = period.EmployeeId,
            LeaveTypeId      = leaveType.Id,
            StartDate        = startDate,
            EndDate          = endDate,
            TotalDays        = totalDays,
            Reason           = request.Reason ?? $"Created by admin on behalf of employee for attendance period {period.StartDate:dd MMM} - {period.EndDate:dd MMM yyyy}",
            Status           = "Approved",
            RequestedOn      = DateTime.UtcNow,
            ApprovedBy       = adminUser.Id,
            ApprovedOn       = DateTime.UtcNow,
            ApprovalRemarks  = $"Auto-approved by {adminUser.Username} on behalf of employee"
        };

        _db.LeaveRequests.Add(leaveRequest);

        // Deduct from leave balance
        var balance = await _db.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == period.EmployeeId
                                   && b.LeaveTypeId == leaveType.Id
                                   && b.Year == startDate.Year);
        if (balance != null)
        {
            balance.UsedDays    += totalDays;
            balance.BalanceDays  = balance.TotalDays + balance.CarryForwardDays - balance.UsedDays;
            balance.UpdatedAt    = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin {Admin} created leave on behalf of employee {EmpId}: {Type} {Start}-{End}",
            adminUser.Username, period.EmployeeId, request.LeaveTypeCode, startDate, endDate);

        return Ok(new
        {
            message      = $"{leaveType.Name} leave created and approved on behalf of {period.Employee.Name}",
            leaveRequestId = leaveRequest.Id,
            totalDays,
            leaveType    = leaveType.Name
        });
    }
}

/// <summary>
/// Request model for rejecting attendance
/// </summary>
public class RejectAttendanceRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}

public class CreateLeaveOnBehalfRequest
{
    public string   LeaveTypeCode { get; set; } = "AL";
    public DateTime StartDate     { get; set; }
    public DateTime EndDate       { get; set; }
    public string?  Reason        { get; set; }
}

// ── Email body helpers ────────────────────────────────────────────────────────
public static partial class AttendanceEmailBodies
{
    public static string BuildAttendanceApprovedBody(string name, DateTime start, DateTime end) =>
        $@"<!DOCTYPE html><html><head><style>
          body{{font-family:Arial,sans-serif;color:#333;}}
          .header{{background:linear-gradient(135deg,#10b981,#059669);color:white;padding:28px;text-align:center;border-radius:8px 8px 0 0;}}
          .header h1{{margin:0;font-size:22px;}} .body{{background:#f8f9fa;padding:28px;border-radius:0 0 8px 8px;}}
          .box{{background:white;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin:16px 0;font-size:14px;}}
          .notice{{background:#f0fdf4;border:1px solid #bbf7d0;color:#166534;padding:10px 14px;border-radius:8px;font-size:13px;margin-top:12px;}}
          .footer{{text-align:center;font-size:12px;color:#888;margin-top:20px;}}
        </style></head><body>
          <div class=""header""><h1>✅ Attendance Approved</h1></div>
          <div class=""body"">
            <p>Dear <strong>{name}</strong>, your attendance period has been <strong>approved</strong>.</p>
            <div class=""box""><strong>Period:</strong> {start:dd MMM yyyy} – {end:dd MMM yyyy}</div>
            <div class=""notice"">No further action is required.</div>
          </div>
          <div class=""footer"">© {DateTime.Now.Year} HRMS. This is an automated email.</div>
        </body></html>";

    public static string BuildAttendanceRejectedBody(string name, DateTime start, DateTime end, string reason) =>
        $@"<!DOCTYPE html><html><head><style>
          body{{font-family:Arial,sans-serif;color:#333;}}
          .header{{background:linear-gradient(135deg,#ef4444,#dc2626);color:white;padding:28px;text-align:center;border-radius:8px 8px 0 0;}}
          .header h1{{margin:0;font-size:22px;}} .body{{background:#f8f9fa;padding:28px;border-radius:0 0 8px 8px;}}
          .box{{background:white;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin:16px 0;font-size:14px;}}
          .notice{{background:#fff7ed;border:1px solid #fed7aa;color:#9a3412;padding:10px 14px;border-radius:8px;font-size:13px;margin-top:12px;}}
          .footer{{text-align:center;font-size:12px;color:#888;margin-top:20px;}}
        </style></head><body>
          <div class=""header""><h1>⚠️ Attendance Rejected</h1></div>
          <div class=""body"">
            <p>Dear <strong>{name}</strong>, your attendance period has been <strong>rejected</strong>.</p>
            <div class=""box"">
              <p><strong>Period:</strong> {start:dd MMM yyyy} – {end:dd MMM yyyy}</p>
              <p><strong>Reason:</strong> {reason}</p>
            </div>
            <div class=""notice"">Please amend your attendance and resubmit.</div>
          </div>
          <div class=""footer"">© {DateTime.Now.Year} HRMS. This is an automated email.</div>
        </body></html>";
}
