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

    public AttendanceManagementController(HrmsDbContext db, IEmailService emailService, ILogger<AttendanceManagementController> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
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

        // Send email notification
        try
        {
            await _emailService.SendAttendanceApprovedEmailAsync(
                period.Employee.Email,
                period.Employee.Name,
                period.StartDate,
                period.EndDate
            );
            _logger.LogInformation("Attendance approved email sent to {Email} for period {PeriodId}", period.Employee.Email, period.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send attendance approved email to {Email}", period.Employee.Email);
            // Don't fail the approval if email fails
        }

        return Ok(new
        {
            message = "Attendance period approved successfully",
            periodId = period.Id,
            status = period.Status,
            approvedAt = period.ApprovedAt,
            emailSent = true
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

        // Send email notification
        try
        {
            await _emailService.SendAttendanceRejectedEmailAsync(
                period.Employee.Email,
                period.Employee.Name,
                period.StartDate,
                period.EndDate,
                request.RejectionReason
            );
            _logger.LogInformation("Attendance rejected email sent to {Email} for period {PeriodId}", period.Employee.Email, period.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send attendance rejected email to {Email}", period.Employee.Email);
            // Don't fail the rejection if email fails
        }

        return Ok(new
        {
            message = "Attendance period rejected successfully",
            periodId = period.Id,
            status = period.Status,
            rejectedAt = period.RejectedAt,
            rejectionReason = period.RejectionReason,
            emailSent = true
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
