using HrmsApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(HrmsDbContext db, ILogger<DashboardController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/dashboard/admin - Get admin dashboard data with overview and pending approvals
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;

        // 1. Employee Statistics
        var totalEmployees = await _db.Employees.CountAsync();
        var activeEmployees = await _db.Employees.CountAsync(e => e.IsActive);
        var inactiveEmployees = totalEmployees - activeEmployees;
        var totalDepartments = await _db.Departments.CountAsync();

        // 2. Pending Approvals Summary
        var pendingLeaveRequests = await _db.LeaveRequests
            .Where(lr => lr.Status == "Pending")
            .CountAsync();

        var pendingAttendancePeriods = await _db.AttendancePeriods
            .Where(ap => ap.Status == "Submitted")
            .CountAsync();

        var pendingTimesheets = await _db.Timesheets
            .Where(t => t.Status == "Submitted")
            .CountAsync();

        // 3. Recent Leave Requests (Last 10)
        var recentLeaveRequests = await _db.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .OrderByDescending(lr => lr.RequestedOn)
            .Take(10)
            .Select(lr => new
            {
                lr.Id,
                employeeName = lr.Employee.Name,
                employeeCode = lr.Employee.EmployeeCode,
                leaveType = lr.LeaveType.Name,
                leaveTypeCode = lr.LeaveType.Code,
                lr.StartDate,
                lr.EndDate,
                lr.TotalDays,
                lr.Status,
                lr.RequestedOn
            })
            .ToListAsync();

        // 4. Recent Attendance Submissions (Last 10)
        var recentAttendance = await _db.AttendancePeriods
            .Include(ap => ap.Employee)
            .OrderByDescending(ap => ap.SubmittedAt ?? ap.CreatedAt)
            .Take(10)
            .Select(ap => new
            {
                ap.Id,
                employeeName = ap.Employee.Name,
                employeeCode = ap.Employee.EmployeeCode,
                ap.StartDate,
                ap.EndDate,
                periodLabel = ap.StartDate.ToString("MMM dd") + " - " + ap.EndDate.ToString("MMM dd, yyyy"),
                ap.Status,
                submittedAt = ap.SubmittedAt ?? ap.CreatedAt
            })
            .ToListAsync();

        // 5. Current Month Payroll Summary
        var currentMonthPayrolls = await _db.Payrolls
            .Where(p => p.Month == currentMonth && p.Year == currentYear)
            .GroupBy(p => p.Status)
            .Select(g => new
            {
                status = g.Key,
                count = g.Count(),
                totalAmount = g.Sum(p => p.NetSalary)
            })
            .ToListAsync();

        var totalPayrollCount = await _db.Payrolls
            .Where(p => p.Month == currentMonth && p.Year == currentYear)
            .CountAsync();

        var totalPayrollAmount = await _db.Payrolls
            .Where(p => p.Month == currentMonth && p.Year == currentYear)
            .SumAsync(p => (decimal?)p.NetSalary) ?? 0;

        // 6. Department-wise Employee Count
        var departmentStats = await _db.Employees
            .Include(e => e.Department)
            .Where(e => e.IsActive)
            .GroupBy(e => e.Department.Name)
            .Select(g => new
            {
                departmentName = g.Key,
                employeeCount = g.Count()
            })
            .OrderByDescending(d => d.employeeCount)
            .ToListAsync();

        // 7. Leave Type Usage This Year
        var leaveUsageStats = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.StartDate.Year == currentYear && lr.Status == "Approved")
            .GroupBy(lr => new { lr.LeaveType.Name, lr.LeaveType.Code })
            .Select(g => new
            {
                leaveType = g.Key.Name,
                leaveTypeCode = g.Key.Code,
                totalDays = g.Sum(lr => lr.TotalDays),
                requestCount = g.Count()
            })
            .OrderByDescending(l => l.totalDays)
            .ToListAsync();

        // 8. Recent Activities (Combined)
        var recentActivities = new List<object>();

        // Add recent approvals
        var recentApprovals = await _db.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.Status == "Approved" && lr.ApprovedOn.HasValue)
            .OrderByDescending(lr => lr.ApprovedOn)
            .Take(5)
            .Select(lr => new
            {
                type = "leave_approval",
                employeeName = lr.Employee.Name,
                description = lr.LeaveType.Name + " approved",
                timestamp = lr.ApprovedOn.Value,
                icon = "check-circle"
            })
            .ToListAsync();

        recentActivities.AddRange(recentApprovals);

        // Add recent attendance approvals
        var recentAttendanceApprovals = await _db.AttendancePeriods
            .Include(ap => ap.Employee)
            .Where(ap => ap.Status == "Approved" && ap.ApprovedAt.HasValue)
            .OrderByDescending(ap => ap.ApprovedAt)
            .Take(5)
            .Select(ap => new
            {
                type = "attendance_approval",
                employeeName = ap.Employee.Name,
                description = "Attendance approved",
                timestamp = ap.ApprovedAt.Value,
                icon = "calendar-check"
            })
            .ToListAsync();

        recentActivities.AddRange(recentAttendanceApprovals);

        // Sort by timestamp and take top 10
        var sortedActivities = recentActivities
            .OrderByDescending(a => ((dynamic)a).timestamp)
            .Take(10)
            .ToList();

        // 9. Quick Stats
        var stats = new
        {
            totalEmployees,
            activeEmployees,
            inactiveEmployees,
            totalDepartments,
            pendingApprovals = pendingLeaveRequests + pendingAttendancePeriods + pendingTimesheets,
            pendingLeaves = pendingLeaveRequests,
            pendingAttendance = pendingAttendancePeriods,
            pendingTimesheets = pendingTimesheets,
            currentMonthPayrolls = totalPayrollCount,
            currentMonthPayrollAmount = totalPayrollAmount
        };

        return Ok(new
        {
            stats,
            pendingCounts = new
            {
                leaveRequests = pendingLeaveRequests,
                attendancePeriods = pendingAttendancePeriods,
                timesheets = pendingTimesheets,
                total = pendingLeaveRequests + pendingAttendancePeriods + pendingTimesheets
            },
            recentLeaveRequests,
            recentAttendance,
            currentMonthPayrolls,
            departmentStats,
            leaveUsageStats,
            recentActivities = sortedActivities
        });
    }
}
