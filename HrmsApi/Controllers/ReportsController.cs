using HrmsApi.Data;
using HrmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly IExportService _exportService;

    public ReportsController(HrmsDbContext db, IExportService exportService)
    {
        _db = db;
        _exportService = exportService;
    }

    /// <summary>
    /// GET /api/reports/department-payroll-summary?year=2026&month=5
    /// Returns payroll summary grouped by department for a specific month
    /// </summary>
    [HttpGet("department-payroll-summary")]
    public async Task<IActionResult> GetDepartmentPayrollSummary([FromQuery] int year, [FromQuery] int month)
    {
        var payrolls = await _db.Payrolls
            .Include(p => p.Employee)
            .ThenInclude(e => e.Department)
            .Where(p => p.Year == year && p.Month == month)
            .ToListAsync();

        var summary = payrolls
            .GroupBy(p => new
            {
                DepartmentId = p.Employee.DepartmentId,
                DepartmentName = p.Employee.Department != null ? p.Employee.Department.Name : "Unassigned"
            })
            .Select(g => new
            {
                departmentId = g.Key.DepartmentId,
                departmentName = g.Key.DepartmentName,
                employeeCount = g.Count(),
                totalGross = g.Sum(p => p.GrossIncome),
                totalNet = g.Sum(p => p.NetSalary),
                totalEpf = g.Sum(p => p.EpfAmount),
                totalSocso = g.Sum(p => p.SocsoAmount),
                totalTax = g.Sum(p => p.TaxAmount),
                totalDeductions = g.Sum(p => p.Deductions),
                averageSalary = g.Average(p => p.NetSalary)
            })
            .OrderByDescending(x => x.totalNet)
            .ToList();

        return Ok(new
        {
            year,
            month,
            monthName = new DateTime(year, month, 1).ToString("MMMM yyyy"),
            departments = summary,
            totals = new
            {
                totalEmployees = payrolls.Count,
                totalGross = payrolls.Sum(p => p.GrossIncome),
                totalNet = payrolls.Sum(p => p.NetSalary),
                totalEpf = payrolls.Sum(p => p.EpfAmount),
                totalSocso = payrolls.Sum(p => p.SocsoAmount),
                totalTax = payrolls.Sum(p => p.TaxAmount),
                totalDeductions = payrolls.Sum(p => p.Deductions)
            }
        });
    }

    /// <summary>
    /// GET /api/reports/attendance-by-date-range?startDate=2026-05-01&endDate=2026-05-31
    /// Returns attendance records for a date range
    /// </summary>
    [HttpGet("attendance-by-date-range")]
    public async Task<IActionResult> GetAttendanceByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var records = await _db.Attendances
            .Include(a => a.Employee)
            .ThenInclude(e => e.Department)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Employee.Name)
            .Select(a => new
            {
                a.Id,
                a.Date,
                employeeId = a.EmployeeId,
                employeeName = a.Employee.Name,
                departmentName = a.Employee.Department != null ? a.Employee.Department.Name : "Unassigned",
                a.Status,
                a.CheckIn,
                a.CheckOut,
                a.WorkHours,
                a.Remarks
            })
            .ToListAsync();

        var summary = new
        {
            startDate,
            endDate,
            totalRecords = records.Count,
            presentCount = records.Count(r => r.Status == "Present"),
            absentCount = records.Count(r => r.Status == "Absent"),
            leaveCount = records.Count(r => r.Status == "Leave"),
            halfDayCount = records.Count(r => r.Status == "HalfDay"),
            
            byDepartment = records
                .GroupBy(r => r.departmentName)
                .Select(g => new
                {
                    department = g.Key,
                    totalRecords = g.Count(),
                    presentCount = g.Count(r => r.Status == "Present"),
                    absentCount = g.Count(r => r.Status == "Absent"),
                    leaveCount = g.Count(r => r.Status == "Leave"),
                    halfDayCount = g.Count(r => r.Status == "HalfDay")
                })
                .OrderByDescending(x => x.totalRecords)
                .ToList(),
            
            records
        };

        return Ok(summary);
    }

    /// <summary>
    /// GET /api/reports/employee-directory
    /// Returns employee directory with photos and contact information
    /// </summary>
    [HttpGet("employee-directory")]
    public async Task<IActionResult> GetEmployeeDirectory([FromQuery] bool includeInactive = false)
    {
        var query = _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Bank)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(e => e.IsActive);

        var employees = await query
            .OrderBy(e => e.Department!.Name)
            .ThenBy(e => e.Name)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Email,
                e.Phone,
                departmentName = e.Department != null ? e.Department.Name : "Unassigned",
                e.Designation,
                e.JoinDate,
                e.Salary,
                e.IsActive,
                e.ProfilePicture,
                bankName = e.Bank != null ? e.Bank.Name : null,
                e.AccountNumber
            })
            .ToListAsync();

        var byDepartment = employees
            .GroupBy(e => e.departmentName)
            .Select(g => new
            {
                department = g.Key,
                employeeCount = g.Count(),
                employees = g.ToList()
            })
            .OrderBy(x => x.department)
            .ToList();

        return Ok(new
        {
            totalEmployees = employees.Count,
            activeEmployees = employees.Count(e => e.IsActive),
            inactiveEmployees = employees.Count(e => !e.IsActive),
            byDepartment,
            allEmployees = employees
        });
    }

    /// <summary>
    /// GET /api/reports/payroll-history?employeeId=1&months=12
    /// Returns payroll history for a specific employee
    /// </summary>
    [HttpGet("payroll-history")]
    public async Task<IActionResult> GetPayrollHistory([FromQuery] int employeeId, [FromQuery] int months = 12)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-months);

        var payrolls = await _db.Payrolls
            .Include(p => p.Employee)
            .Where(p => p.EmployeeId == employeeId)
            .Where(p => p.GeneratedOn >= cutoffDate)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new
            {
                p.Id,
                p.Month,
                p.Year,
                monthName = new DateTime(p.Year, p.Month, 1).ToString("MMM yyyy"),
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

        var employee = await _db.Employees.FindAsync(employeeId);

        return Ok(new
        {
            employeeId,
            employeeName = employee?.Name ?? "Unknown",
            recordCount = payrolls.Count,
            averageNetSalary = payrolls.Any() ? payrolls.Average(p => p.NetSalary) : 0,
            totalPaid = payrolls.Sum(p => p.NetSalary),
            records = payrolls
        });
    }

    /// <summary>
    /// GET /api/reports/monthly-summary?year=2026&month=5
    /// Returns comprehensive monthly summary (payroll + attendance)
    /// </summary>
    [HttpGet("monthly-summary")]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int year, [FromQuery] int month)
    {
        // Payroll data
        var payrolls = await _db.Payrolls
            .Where(p => p.Year == year && p.Month == month)
            .ToListAsync();

        // Attendance data for the month
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _db.Attendances
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .ToListAsync();

        var summary = new
        {
            year,
            month,
            monthName = new DateTime(year, month, 1).ToString("MMMM yyyy"),
            
            payroll = new
            {
                totalPayslips = payrolls.Count,
                totalGross = payrolls.Sum(p => p.GrossIncome),
                totalNet = payrolls.Sum(p => p.NetSalary),
                totalEpf = payrolls.Sum(p => p.EpfAmount),
                totalSocso = payrolls.Sum(p => p.SocsoAmount),
                totalTax = payrolls.Sum(p => p.TaxAmount),
                averageSalary = payrolls.Any() ? payrolls.Average(p => p.NetSalary) : 0
            },
            
            attendance = new
            {
                totalRecords = attendances.Count,
                presentCount = attendances.Count(a => a.Status == "Present"),
                absentCount = attendances.Count(a => a.Status == "Absent"),
                leaveCount = attendances.Count(a => a.Status == "Leave"),
                halfDayCount = attendances.Count(a => a.Status == "HalfDay"),
                averageWorkHours = attendances.Where(a => a.WorkHours > 0).Any()
                    ? attendances.Where(a => a.WorkHours > 0).Average(a => a.WorkHours)
                    : 0
            }
        };

        return Ok(summary);
    }

    /// <summary>
    /// GET /api/reports/export/payroll-excel?year=2026&month=5
    /// Exports payroll to Excel file
    /// </summary>
    [HttpGet("export/payroll-excel")]
    public async Task<IActionResult> ExportPayrollToExcel([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var fileBytes = await _exportService.ExportPayrollToExcelAsync(year, month);
            var fileName = $"Payroll_{month}_{year}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Export failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// GET /api/reports/export/employees-excel
    /// Exports employee directory to Excel file
    /// </summary>
    [HttpGet("export/employees-excel")]
    public async Task<IActionResult> ExportEmployeesToExcel()
    {
        try
        {
            var fileBytes = await _exportService.ExportEmployeesToExcelAsync();
            var fileName = $"Employees_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Export failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// GET /api/reports/export/attendance-excel?startDate=2026-05-01&endDate=2026-05-31
    /// Exports attendance records to Excel file
    /// </summary>
    [HttpGet("export/attendance-excel")]
    public async Task<IActionResult> ExportAttendanceToExcel(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var fileBytes = await _exportService.ExportAttendanceToExcelAsync(startDate, endDate);
            var fileName = $"Attendance_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Export failed: {ex.Message}" });
        }
    }
}
