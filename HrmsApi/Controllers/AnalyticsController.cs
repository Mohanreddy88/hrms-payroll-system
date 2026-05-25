using HrmsApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly HrmsDbContext _db;

    public AnalyticsController(HrmsDbContext db) => _db = db;

    /// <summary>
    /// GET /api/analytics/employee-count-by-department
    /// Returns employee count grouped by department for pie chart
    /// </summary>
    [HttpGet("employee-count-by-department")]
    public async Task<IActionResult> GetEmployeeCountByDepartment()
    {
        var data = await _db.Employees
            .Where(e => e.IsActive)
            .GroupBy(e => new { e.DepartmentId, e.Department!.Name })
            .Select(g => new
            {
                departmentId = g.Key.DepartmentId,
                departmentName = g.Key.Name ?? "Unassigned",
                employeeCount = g.Count()
            })
            .OrderByDescending(x => x.employeeCount)
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>
    /// GET /api/analytics/payroll-trends?months=6
    /// Returns monthly payroll totals for the last N months (default 6)
    /// </summary>
    [HttpGet("payroll-trends")]
    public async Task<IActionResult> GetPayrollTrends([FromQuery] int months = 6)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-months);

        var data = await _db.Payrolls
            .Where(p => p.GeneratedOn >= cutoffDate)
            .GroupBy(p => new { p.Year, p.Month })
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                monthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                totalGross = g.Sum(p => p.GrossIncome),
                totalNet = g.Sum(p => p.NetSalary),
                totalDeductions = g.Sum(p => p.EpfAmount + p.SocsoAmount + p.TaxAmount + p.Deductions),
                employeeCount = g.Count()
            })
            .OrderBy(x => x.year).ThenBy(x => x.month)
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>
    /// GET /api/analytics/attendance-statistics?year=2026&month=5
    /// Returns attendance statistics for a given period
    /// - If month is provided: returns stats for that specific month
    /// - If only year is provided: returns stats for the entire year
    /// Uses AttendancePeriodDays table for daily attendance data
    /// </summary>
    [HttpGet("attendance-statistics")]
    public async Task<IActionResult> GetAttendanceStatistics([FromQuery] int? year, [FromQuery] int? month)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        
        DateTime startDate;
        DateTime endDate;
        
        if (month.HasValue)
        {
            // Monthly statistics
            startDate = new DateTime(targetYear, month.Value, 1);
            endDate = startDate.AddMonths(1).AddDays(-1);
        }
        else
        {
            // Yearly statistics (entire year)
            startDate = new DateTime(targetYear, 1, 1);
            endDate = new DateTime(targetYear, 12, 31);
        }

        // Query AttendancePeriodDays for daily attendance records
        var records = await _db.AttendancePeriodDays
            .Include(apd => apd.AttendancePeriod)
                .ThenInclude(ap => ap.Employee)
                    .ThenInclude(e => e.Department)
            .Where(apd => apd.Date >= startDate && apd.Date <= endDate)
            .Where(apd => apd.AttendancePeriod.Status == "Approved") // Only count approved attendance
            .ToListAsync();

        // Calculate statistics based on hours worked
        // Assume: 8+ hours = Present, 4-7.99 hours = HalfDay, 0 hours = Absent/Leave
        var presentCount = records.Count(r => r.Hours >= 8.0m);
        var halfDayCount = records.Count(r => r.Hours > 0 && r.Hours < 8.0m);
        var absentCount = records.Count(r => r.Hours == 0 && !r.IsWeekend && !r.IsPublicHoliday);
        var leaveCount = records.Count(r => r.Hours == 0 && !r.IsWeekend && !r.IsPublicHoliday);

        // Group by department and employee to calculate employee-wise stats
        var byDepartment = records
            .GroupBy(r => new 
            { 
                Department = r.AttendancePeriod.Employee.Department?.Name ?? "Unassigned",
                EmployeeId = r.AttendancePeriod.EmployeeId
            })
            .GroupBy(g => g.Key.Department)
            .Select(deptGroup => new
            {
                department = deptGroup.Key,
                employeeCount = deptGroup.Count(), // Number of employees in this department
                totalDays = deptGroup.Sum(g => g.Count()), // Total attendance days across all employees
                presentDays = deptGroup.Sum(g => g.Count(r => r.Hours >= 8.0m)), // Total present days
                attendanceRate = deptGroup.Sum(g => g.Count()) > 0 
                    ? Math.Round((decimal)deptGroup.Sum(g => g.Count(r => r.Hours >= 8.0m)) / deptGroup.Sum(g => g.Count()) * 100, 1)
                    : 0
            })
            .OrderByDescending(x => x.employeeCount)
            .ToList();

        var statistics = new
        {
            year = targetYear,
            month = month ?? 0, // 0 indicates yearly stats
            totalRecords = records.Count,
            presentCount = presentCount,
            absentCount = absentCount,
            leaveCount = leaveCount,
            halfDayCount = halfDayCount,
            byDepartment = byDepartment
        };

        return Ok(statistics);
    }

    /// <summary>
    /// GET /api/analytics/dashboard-summary
    /// Returns overall statistics for dashboard cards
    /// </summary>
    [HttpGet("dashboard-summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        var summary = new
        {
            totalEmployees = await _db.Employees.CountAsync(e => e.IsActive),
            totalDepartments = await _db.Departments.CountAsync(d => d.IsActive),
            
            monthlyPayroll = await _db.Payrolls
                .Where(p => p.Month == currentMonth && p.Year == currentYear)
                .SumAsync(p => p.NetSalary),
            
            monthlyPayslipCount = await _db.Payrolls
                .CountAsync(p => p.Month == currentMonth && p.Year == currentYear),
            
            todayAttendance = await _db.Attendances
                .Where(a => a.Date.Date == DateTime.Today)
                .CountAsync(a => a.Status == "Present"),
            
            todayAbsent = await _db.Attendances
                .Where(a => a.Date.Date == DateTime.Today)
                .CountAsync(a => a.Status == "Absent")
        };

        return Ok(summary);
    }

    /// <summary>
    /// GET /api/analytics/salary-distribution
    /// Returns salary ranges distribution for analysis
    /// </summary>
    [HttpGet("salary-distribution")]
    public async Task<IActionResult> GetSalaryDistribution()
    {
        var employees = await _db.Employees
            .Where(e => e.IsActive)
            .Select(e => e.Salary)
            .ToListAsync();

        var distribution = new
        {
            below3k = employees.Count(s => s < 3000),
            range3kTo5k = employees.Count(s => s >= 3000 && s < 5000),
            range5kTo8k = employees.Count(s => s >= 5000 && s < 8000),
            range8kTo12k = employees.Count(s => s >= 8000 && s < 12000),
            above12k = employees.Count(s => s >= 12000),
            
            averageSalary = employees.Any() ? employees.Average() : 0,
            medianSalary = employees.Any() ? employees.OrderBy(s => s).ElementAt(employees.Count / 2) : 0,
            minSalary = employees.Any() ? employees.Min() : 0,
            maxSalary = employees.Any() ? employees.Max() : 0
        };

        return Ok(distribution);
    }
}
