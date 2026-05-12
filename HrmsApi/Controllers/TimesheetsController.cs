using HrmsApi.Data;
using HrmsApi.Models;
using HrmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimesheetsController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly ITimesheetService _timesheetService;
    private readonly IEmailService _emailService;
    private readonly IExportService _exportService;

    public TimesheetsController(HrmsDbContext db, ITimesheetService timesheetService, IEmailService emailService, IExportService exportService)
    {
        _db = db;
        _timesheetService = timesheetService;
        _emailService = emailService;
        _exportService = exportService;
    }

    /// <summary>
    /// GET /api/timesheets - Get all timesheets
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? month)
    {
        var query = _db.Timesheets.Include(t => t.Employee).AsQueryable();

        if (year.HasValue)
            query = query.Where(t => t.Year == year.Value);

        if (month.HasValue)
            query = query.Where(t => t.Month == month.Value);

        var timesheets = await query
            .OrderByDescending(t => t.Year)
            .ThenByDescending(t => t.Month)
            .ThenBy(t => t.Employee.Name)
            .Select(t => new
            {
                t.Id,
                t.EmployeeId,
                employeeName = t.Employee.Name,
                t.Month,
                t.Year,
                monthName = new DateTime(t.Year, t.Month, 1).ToString("MMMM yyyy"),
                t.TotalWorkingDays,
                t.TotalPresent,
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
    /// GET /api/timesheets/{id} - Get timesheet by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var timesheet = await _db.Timesheets
            .Include(t => t.Employee)
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id,
                t.EmployeeId,
                employeeName = t.Employee.Name,
                t.Month,
                t.Year,
                monthName = new DateTime(t.Year, t.Month, 1).ToString("MMMM yyyy"),
                t.TotalWorkingDays,
                t.TotalPresent,
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
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Timesheet {id} not found");

        return Ok(timesheet);
    }

    /// <summary>
    /// GET /api/timesheets/employee/{employeeId} - Get timesheets for an employee
    /// </summary>
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId, [FromQuery] int? year)
    {
        var query = _db.Timesheets
            .Include(t => t.Employee)
            .Where(t => t.EmployeeId == employeeId);

        if (year.HasValue)
            query = query.Where(t => t.Year == year.Value);

        var timesheets = await query
            .OrderByDescending(t => t.Year)
            .ThenByDescending(t => t.Month)
            .ToListAsync();

        return Ok(timesheets);
    }

    /// <summary>
    /// POST /api/timesheets/generate - Generate timesheet for an employee
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateTimesheet([FromBody] TimesheetRequest request)
    {
        try
        {
            var timesheet = await _timesheetService.GenerateTimesheetAsync(
                request.EmployeeId, 
                request.Month, 
                request.Year
            );

            return Ok(timesheet);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/timesheets/generate-all - Generate timesheets for ALL employees
    /// </summary>
    [HttpPost("generate-all")]
    public async Task<IActionResult> GenerateAllTimesheets([FromQuery] int month, [FromQuery] int year)
    {
        var timesheets = await _timesheetService.GenerateTimesheetsForAllEmployeesAsync(month, year);
        
        return Ok(new
        {
            message = $"Generated {timesheets.Count} timesheets for {month}/{year}",
            count = timesheets.Count,
            timesheets
        });
    }

    /// <summary>
    /// PUT /api/timesheets/{id}/submit - Submit timesheet for approval
    /// </summary>
    [HttpPut("{id:int}/submit")]
    public async Task<IActionResult> SubmitTimesheet(int id)
    {
        var timesheet = await _db.Timesheets.FindAsync(id)
            ?? throw new KeyNotFoundException($"Timesheet {id} not found");

        if (timesheet.Status != "Draft")
            return BadRequest(new { message = $"Cannot submit timesheet with status {timesheet.Status}" });

        timesheet.Status = "Submitted";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Timesheet submitted for approval" });
    }

    /// <summary>
    /// PUT /api/timesheets/{id}/approve - Approve timesheet
    /// </summary>
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveTimesheet(int id, [FromBody] ApprovalRequest approval)
    {
        var timesheet = await _db.Timesheets.FindAsync(id)
            ?? throw new KeyNotFoundException($"Timesheet {id} not found");

        if (timesheet.Status != "Submitted")
            return BadRequest(new { message = $"Can only approve submitted timesheets" });

        // 1. Confirm pending attendance records (change remarks from PENDING to APPROVED)
        var startDate = new DateTime(timesheet.Year, timesheet.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        
        var pendingAttendances = await _db.Attendances
            .Where(a => a.EmployeeId == timesheet.EmployeeId 
                && a.Date >= startDate 
                && a.Date <= endDate
                && a.Remarks != null 
                && a.Remarks.Contains("PENDING APPROVAL"))
            .ToListAsync();

        int confirmedCount = 0;
        decimal totalLeaveDeducted = 0;

        foreach (var attendance in pendingAttendances)
        {
            attendance.Remarks = $"APPROVED by Admin - {DateTime.Now:yyyy-MM-dd}";
            confirmedCount++;

            // Count leave days for deduction
            if (attendance.Status == "Leave")
                totalLeaveDeducted += 1;
            else if (attendance.Status == "HalfDay")
                totalLeaveDeducted += 0.5m;
        }

        // 2. Deduct leave balance from Annual Leave
        if (totalLeaveDeducted > 0)
        {
            var annualLeaveType = await _db.LeaveTypes.FirstOrDefaultAsync(lt => lt.Code == "AL");
            if (annualLeaveType != null)
            {
                var leaveBalance = await _db.EmployeeLeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == timesheet.EmployeeId 
                        && lb.LeaveTypeId == annualLeaveType.Id 
                        && lb.Year == timesheet.Year);

                if (leaveBalance != null)
                {
                    leaveBalance.UsedDays += totalLeaveDeducted;
                    leaveBalance.BalanceDays = leaveBalance.TotalDays - leaveBalance.UsedDays;
                    leaveBalance.UpdatedAt = DateTime.Now;
                }
            }
        }

        // 3. Update timesheet status
        timesheet.Status = "Approved";
        timesheet.ApprovedBy = approval.ApprovedByUserId;
        timesheet.ApprovedOn = DateTime.UtcNow;
        timesheet.Remarks = approval.Remarks;

        await _db.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Timesheet approved! Attendance records confirmed and leave balance updated.",
            attendanceRecordsConfirmed = confirmedCount,
            leaveDaysDeducted = totalLeaveDeducted
        });
    }

    /// <summary>
    /// PUT /api/timesheets/{id}/reject - Reject timesheet
    /// </summary>
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectTimesheet(int id, [FromBody] ApprovalRequest approval)
    {
        var timesheet = await _db.Timesheets.FindAsync(id)
            ?? throw new KeyNotFoundException($"Timesheet {id} not found");

        if (timesheet.Status != "Submitted")
            return BadRequest(new { message = $"Can only reject submitted timesheets" });

        // Delete pending attendance records since timesheet is rejected
        var startDate = new DateTime(timesheet.Year, timesheet.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        
        var pendingAttendances = await _db.Attendances
            .Where(a => a.EmployeeId == timesheet.EmployeeId 
                && a.Date >= startDate 
                && a.Date <= endDate
                && a.Remarks != null 
                && a.Remarks.Contains("PENDING APPROVAL"))
            .ToListAsync();

        int deletedCount = pendingAttendances.Count;
        _db.Attendances.RemoveRange(pendingAttendances);

        timesheet.Status = "Rejected";
        timesheet.ApprovedBy = approval.ApprovedByUserId;
        timesheet.ApprovedOn = DateTime.UtcNow;
        timesheet.Remarks = approval.Remarks;

        await _db.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Timesheet rejected. Pending attendance records deleted. Employee can resubmit.",
            attendanceRecordsDeleted = deletedCount
        });
    }

    /// <summary>
    /// DELETE /api/timesheets/{id} - Delete timesheet
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var timesheet = await _db.Timesheets.FindAsync(id)
            ?? throw new KeyNotFoundException($"Timesheet {id} not found");

        if (timesheet.Status == "Approved")
            return BadRequest(new { message = "Cannot delete approved timesheets" });

        _db.Timesheets.Remove(timesheet);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// GET /api/timesheets/working-days - Calculate working days in a month
    /// </summary>
    [HttpGet("working-days")]
    public async Task<IActionResult> GetWorkingDays([FromQuery] int month, [FromQuery] int year)
    {
        var workingDays = await _timesheetService.GetWorkingDaysInMonthAsync(month, year);
        var holidays = await _timesheetService.GetPublicHolidaysInMonthAsync(month, year);

        return Ok(new
        {
            month,
            year,
            monthName = new DateTime(year, month, 1).ToString("MMMM yyyy"),
            workingDays,
            publicHolidays = holidays.Count,
            holidays
        });
    }

    /// <summary>
    /// POST /api/timesheets/{id}/email - Email timesheet to employee
    /// </summary>
    [HttpPost("{id:int}/email")]
    public async Task<IActionResult> EmailTimesheet(int id)
    {
        try
        {
            var timesheet = await _db.Timesheets
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new KeyNotFoundException($"Timesheet {id} not found");

            if (string.IsNullOrWhiteSpace(timesheet.Employee.Email))
                return BadRequest(new { message = "Employee email address not found" });

            var monthName = new DateTime(timesheet.Year, timesheet.Month, 1).ToString("MMMM yyyy");
            
            // Generate Excel attachment
            var excelData = await _exportService.ExportTimesheetToExcelAsync(id);
            var fileName = $"Timesheet_{timesheet.Employee.Name.Replace(" ", "_")}_{monthName.Replace(" ", "_")}.xlsx";
            
            var subject = $"Your Timesheet for {monthName}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #1a1a1a; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; }}
        .header {{ background: #f5f5f5; padding: 20px; border-bottom: 1px solid #e0e0e0; }}
        .header h2 {{ margin: 0; color: #1a1a1a; font-size: 18px; }}
        .content {{ padding: 30px; background-color: white; }}
        .content p {{ margin: 10px 0; color: #1a1a1a; font-size: 15px; }}
        table {{ border-collapse: collapse; width: 100%; margin: 20px 0; }}
        th {{ background: #f5f5f5; padding: 12px; text-align: left; font-weight: normal; color: #666; border: 1px solid #e0e0e0; }}
        td {{ padding: 12px; border: 1px solid #e0e0e0; }}
        .value-cell {{ text-align: right; }}
        .total-row {{ background: #f9f9f9; font-weight: bold; }}
        .status {{ display: inline-block; padding: 4px 12px; background: #e8f5e9; color: #2e7d32; border-radius: 3px; font-size: 13px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Timesheet - {monthName}</h2>
        </div>
        <div class=""content"">
            <p>Dear <strong>{timesheet.Employee.Name}</strong>,</p>
            <p>Your timesheet for <strong>{monthName}</strong> is now available.</p>
            
            <table>
                <tr>
                    <th>Description</th>
                    <th class=""value-cell"">Value</th>
                </tr>
                <tr>
                    <td>Period</td>
                    <td class=""value-cell"">{monthName}</td>
                </tr>
                <tr>
                    <td>Total Working Days</td>
                    <td class=""value-cell"">{timesheet.TotalWorkingDays}</td>
                </tr>
                <tr>
                    <td>Present</td>
                    <td class=""value-cell"" style=""color: #10b981; font-weight: bold;"">{timesheet.TotalPresent}</td>
                </tr>
                <tr>
                    <td>Absent</td>
                    <td class=""value-cell"" style=""color: #dc3545;"">{timesheet.TotalAbsent}</td>
                </tr>
                <tr>
                    <td>Leave</td>
                    <td class=""value-cell"" style=""color: #ffc107;"">{timesheet.TotalLeave}</td>
                </tr>
                <tr>
                    <td>Half Day</td>
                    <td class=""value-cell"">{timesheet.TotalHalfDay}</td>
                </tr>
                <tr>
                    <td>Public Holidays</td>
                    <td class=""value-cell"">{timesheet.TotalPublicHolidays}</td>
                </tr>
                <tr class=""total-row"">
                    <td>Total Work Hours</td>
                    <td class=""value-cell"" style=""color: #4169E1;"">{timesheet.TotalWorkHours:F1} hours</td>
                </tr>
            </table>
            
            <p><strong>Status:</strong> <span class=""status"">{timesheet.Status}</span></p>
            <p><strong>Generated On:</strong> {timesheet.GeneratedOn:dd MMM yyyy HH:mm}</p>
        </div>
    </div>
</body>
</html>
";

            await _emailService.SendEmailWithAttachmentAsync(timesheet.Employee.Email, subject, body, excelData, fileName);

            return Ok(new { message = $"Timesheet emailed to {timesheet.Employee.Email}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to send email: {ex.Message}" });
        }
    }
}

public class ApprovalRequest
{
    public int ApprovedByUserId { get; set; }
    public string? Remarks { get; set; }
}
