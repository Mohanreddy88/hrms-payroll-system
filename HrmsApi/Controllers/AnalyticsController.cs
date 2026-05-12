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
    /// Returns attendance statistics for a given month (defaults to current month)
    /// </summary>
    [HttpGet("attendance-statistics")]
    public async Task<IActionResult> GetAttendanceStatistics([FromQuery] int? year, [FromQuery] int? month)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;

        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var records = await _db.Attendances
            .Include(a => a.Employee)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .ToListAsync();

        var statistics = new
        {
            year = targetYear,
            month = targetMonth,
            totalRecords = records.Count,
            presentCount = records.Count(r => r.Status == "Present"),
            absentCount = records.Count(r => r.Status == "Absent"),
            leaveCount = records.Count(r => r.Status == "Leave"),
            halfDayCount = records.Count(r => r.Status == "HalfDay"),
            
            // By department
            byDepartment = records
                .GroupBy(r => r.Employee.Department?.Name ?? "Unassigned")
                .Select(g => new
                {
                    department = g.Key,
                    totalRecords = g.Count(),
                    presentCount = g.Count(r => r.Status == "Present")
                })
                .OrderByDescending(x => x.totalRecords)
                .ToList()
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
