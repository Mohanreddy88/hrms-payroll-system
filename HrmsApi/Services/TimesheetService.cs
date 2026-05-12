using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Services;

public interface ITimesheetService
{
    Task<Timesheet> GenerateTimesheetAsync(int employeeId, int month, int year);
    Task<List<Timesheet>> GenerateTimesheetsForAllEmployeesAsync(int month, int year);
    Task<int> GetWorkingDaysInMonthAsync(int month, int year, string? state = null);
    Task<List<DateTime>> GetPublicHolidaysInMonthAsync(int month, int year, string? state = null);
}

public class TimesheetService : ITimesheetService
{
    private readonly HrmsDbContext _db;
    private readonly ILogger<TimesheetService> _logger;

    public TimesheetService(HrmsDbContext db, ILogger<TimesheetService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Generates timesheet for a specific employee for a given month/year
    /// Automatically calculates working days, attendance, and public holidays
    /// </summary>
    public async Task<Timesheet> GenerateTimesheetAsync(int employeeId, int month, int year)
    {
        // Check if timesheet already exists
        var existing = await _db.Timesheets
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Month == month && t.Year == year);

        if (existing != null)
        {
            _logger.LogWarning("Timesheet already exists for Employee {EmployeeId} for {Month}/{Year}", 
                employeeId, month, year);
            throw new InvalidOperationException($"Timesheet already exists for {month}/{year}");
        }

        // Verify employee exists
        var employee = await _db.Employees.FindAsync(employeeId)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        // Calculate date range for the month
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get public holidays for the month (Malaysia)
        var publicHolidays = await GetPublicHolidaysInMonthAsync(month, year);
        var publicHolidayDates = publicHolidays.Select(d => d.Date).ToHashSet();

        // Get attendance records for the employee for this month
        var attendances = await _db.Attendances
            .Where(a => a.EmployeeId == employeeId 
                     && a.Date >= startDate 
                     && a.Date <= endDate)
            .ToListAsync();

        // Calculate totals
        int totalWorkingDays = await GetWorkingDaysInMonthAsync(month, year);
        int totalPresent = attendances.Count(a => a.Status == "Present");
        int totalAbsent = attendances.Count(a => a.Status == "Absent");
        int totalLeave = attendances.Count(a => a.Status == "Leave");
        int totalHalfDay = attendances.Count(a => a.Status == "HalfDay");
        int totalPublicHolidays = publicHolidays.Count;
        decimal totalWorkHours = attendances.Where(a => a.WorkHours > 0).Sum(a => a.WorkHours);

        // Create timesheet
        var timesheet = new Timesheet
        {
            EmployeeId = employeeId,
            Month = month,
            Year = year,
            TotalWorkingDays = totalWorkingDays,
            TotalPresent = totalPresent,
            TotalAbsent = totalAbsent,
            TotalLeave = totalLeave,
            TotalHalfDay = totalHalfDay,
            TotalPublicHolidays = totalPublicHolidays,
            TotalWorkHours = totalWorkHours,
            GeneratedOn = DateTime.UtcNow,
            Status = "Draft"
        };

        _db.Timesheets.Add(timesheet);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Timesheet generated for Employee {EmployeeId} for {Month}/{Year}", 
            employeeId, month, year);

        return timesheet;
    }

    /// <summary>
    /// Generates timesheets for ALL active employees for a given month/year
    /// </summary>
    public async Task<List<Timesheet>> GenerateTimesheetsForAllEmployeesAsync(int month, int year)
    {
        var activeEmployees = await _db.Employees
            .Where(e => e.IsActive)
            .ToListAsync();

        var timesheets = new List<Timesheet>();

        foreach (var employee in activeEmployees)
        {
            try
            {
                var timesheet = await GenerateTimesheetAsync(employee.Id, month, year);
                timesheets.Add(timesheet);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Skipping Employee {EmployeeId}: {Message}", 
                    employee.Id, ex.Message);
                // Skip if already exists
                continue;
            }
        }

        _logger.LogInformation("Generated {Count} timesheets for {Month}/{Year}", 
            timesheets.Count, month, year);

        return timesheets;
    }

    /// <summary>
    /// Calculate total working days in a month (excluding weekends and public holidays)
    /// Malaysia: Monday-Friday = working days (5-day week)
    /// </summary>
    public async Task<int> GetWorkingDaysInMonthAsync(int month, int year, string? state = null)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get public holidays for the month
        var publicHolidays = await GetPublicHolidaysInMonthAsync(month, year, state);
        var publicHolidayDates = publicHolidays.Select(d => d.Date).ToHashSet();

        int workingDays = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Skip weekends (Saturday = 6, Sunday = 0)
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            // Skip public holidays
            if (publicHolidayDates.Contains(date.Date))
                continue;

            workingDays++;
        }

        return workingDays;
    }

    /// <summary>
    /// Get list of public holidays in a specific month (Malaysia)
    /// </summary>
    public async Task<List<DateTime>> GetPublicHolidaysInMonthAsync(int month, int year, string? state = null)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var query = _db.PublicHolidays
            .Where(h => h.Date >= startDate && h.Date <= endDate);

        // Include national holidays
        var holidays = await query
            .Where(h => h.IsNational)
            .ToListAsync();

        // Add state-specific holidays if state is provided
        if (!string.IsNullOrWhiteSpace(state))
        {
            var stateHolidays = await _db.PublicHolidays
                .Where(h => h.Date >= startDate 
                         && h.Date <= endDate 
                         && !h.IsNational
                         && h.State != null 
                         && h.State.Contains(state))
                .ToListAsync();

            holidays.AddRange(stateHolidays);
        }

        return holidays
            .Select(h => h.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();
    }
}
