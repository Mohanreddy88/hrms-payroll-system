using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Services;

public interface ILeaveService
{
    Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequestCreate request);
    Task<LeaveRequest> ApproveLeaveRequestAsync(int leaveRequestId, int approvedByUserId, string remarks);
    Task<LeaveRequest> RejectLeaveRequestAsync(int leaveRequestId, int rejectedByUserId, string remarks);
    Task<LeaveRequest> CancelLeaveRequestAsync(int leaveRequestId, string reason);
    Task<decimal> CalculateLeaveDaysAsync(DateTime startDate, DateTime endDate);
    Task InitializeLeaveBalancesAsync(int employeeId, int year);
    Task<List<EmployeeLeaveBalance>> GetEmployeeLeaveBalancesAsync(int employeeId, int year);
}

public class LeaveService : ILeaveService
{
    private readonly HrmsDbContext _db;
    private readonly ITimesheetService _timesheetService;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        HrmsDbContext db, 
        ITimesheetService timesheetService,
        ILogger<LeaveService> logger)
    {
        _db = db;
        _timesheetService = timesheetService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new leave request
    /// Automatically calculates leave days excluding weekends and public holidays
    /// </summary>
    public async Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequestCreate request)
    {
        // Validate employee exists
        var employee = await _db.Employees.FindAsync(request.EmployeeId)
            ?? throw new KeyNotFoundException($"Employee {request.EmployeeId} not found");

        // Validate leave type exists
        var leaveType = await _db.LeaveTypes.FindAsync(request.LeaveTypeId)
            ?? throw new KeyNotFoundException($"Leave type {request.LeaveTypeId} not found");

        if (!leaveType.IsActive)
            throw new InvalidOperationException($"Leave type '{leaveType.Name}' is not active");

        // Validate dates
        if (request.StartDate > request.EndDate)
            throw new InvalidOperationException("Start date cannot be after end date");

        // Calculate leave days (excluding weekends and public holidays)
        var leaveDays = await CalculateLeaveDaysAsync(request.StartDate, request.EndDate);

        if (leaveDays <= 0)
            throw new InvalidOperationException("Leave request must be for at least 0.5 days");

        // Check leave balance
        var year = request.StartDate.Year;
        var balance = await _db.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId 
                                   && b.LeaveTypeId == request.LeaveTypeId 
                                   && b.Year == year);

        if (balance == null)
        {
            // Initialize balance if not exists
            await InitializeLeaveBalancesAsync(request.EmployeeId, year);
            balance = await _db.EmployeeLeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId 
                                       && b.LeaveTypeId == request.LeaveTypeId 
                                       && b.Year == year);
        }

        if (balance != null && balance.BalanceDays < leaveDays)
        {
            throw new InvalidOperationException(
                $"Insufficient leave balance. Available: {balance.BalanceDays} days, Requested: {leaveDays} days");
        }

        // Check for existing leave request with same dates (only Pending or Draft status)
        var existingRequest = await _db.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.EmployeeId == request.EmployeeId
                                    && lr.StartDate == request.StartDate
                                    && lr.EndDate == request.EndDate
                                    && (lr.Status == "Pending" || lr.Status == "Draft"));

        if (existingRequest != null)
        {
            // Update existing request instead of creating duplicate
            existingRequest.LeaveTypeId = request.LeaveTypeId;
            existingRequest.TotalDays = leaveDays;
            existingRequest.Reason = request.Reason;
            existingRequest.Status = "Pending";
            existingRequest.RequestedOn = DateTime.UtcNow;
            
            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Leave request updated: Id {Id}, Employee {EmployeeId}, Type {LeaveType}, Days {Days}",
                existingRequest.Id, request.EmployeeId, leaveType.Name, leaveDays);
            
            return existingRequest;
        }

        // Create new leave request
        var leaveRequest = new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = leaveDays,
            Reason = request.Reason,
            Status = "Pending",
            RequestedOn = DateTime.UtcNow
        };

        _db.LeaveRequests.Add(leaveRequest);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Leave request created: Employee {EmployeeId}, Type {LeaveType}, Days {Days}",
            request.EmployeeId, leaveType.Name, leaveDays);

        return leaveRequest;
    }

    /// <summary>
    /// Approve a leave request and deduct from employee leave balance
    /// </summary>
    public async Task<LeaveRequest> ApproveLeaveRequestAsync(int leaveRequestId, int approvedByUserId, string remarks)
    {
        var leaveRequest = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Employee)
            .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId)
            ?? throw new KeyNotFoundException($"Leave request {leaveRequestId} not found");

        if (leaveRequest.Status != "Pending")
            throw new InvalidOperationException($"Leave request is already {leaveRequest.Status}");

        // Update leave request status
        leaveRequest.Status = "Approved";
        leaveRequest.ApprovedBy = approvedByUserId;
        leaveRequest.ApprovedOn = DateTime.UtcNow;
        leaveRequest.ApprovalRemarks = remarks;

        // Deduct from leave balance
        var year = leaveRequest.StartDate.Year;
        var balance = await _db.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == leaveRequest.EmployeeId
                                   && b.LeaveTypeId == leaveRequest.LeaveTypeId
                                   && b.Year == year);

        if (balance != null)
        {
            balance.UsedDays += leaveRequest.TotalDays;
            balance.BalanceDays = balance.TotalDays + balance.CarryForwardDays - balance.UsedDays;
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Leave request {Id} approved by User {UserId}", leaveRequestId, approvedByUserId);

        return leaveRequest;
    }

    /// <summary>
    /// Reject a leave request
    /// </summary>
    public async Task<LeaveRequest> RejectLeaveRequestAsync(int leaveRequestId, int rejectedByUserId, string remarks)
    {
        var leaveRequest = await _db.LeaveRequests.FindAsync(leaveRequestId)
            ?? throw new KeyNotFoundException($"Leave request {leaveRequestId} not found");

        if (leaveRequest.Status != "Pending")
            throw new InvalidOperationException($"Leave request is already {leaveRequest.Status}");

        leaveRequest.Status = "Rejected";
        leaveRequest.ApprovedBy = rejectedByUserId;
        leaveRequest.ApprovedOn = DateTime.UtcNow;
        leaveRequest.ApprovalRemarks = remarks;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Leave request {Id} rejected by User {UserId}", leaveRequestId, rejectedByUserId);

        return leaveRequest;
    }

    /// <summary>
    /// Cancel a leave request (can only cancel approved/pending requests)
    /// </summary>
    public async Task<LeaveRequest> CancelLeaveRequestAsync(int leaveRequestId, string reason)
    {
        var leaveRequest = await _db.LeaveRequests.FindAsync(leaveRequestId)
            ?? throw new KeyNotFoundException($"Leave request {leaveRequestId} not found");

        if (leaveRequest.Status == "Cancelled" || leaveRequest.Status == "Rejected")
            throw new InvalidOperationException($"Cannot cancel a {leaveRequest.Status} leave request");

        var wasApproved = leaveRequest.Status == "Approved";

        // If approved, restore leave balance
        if (wasApproved)
        {
            var year = leaveRequest.StartDate.Year;
            var balance = await _db.EmployeeLeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == leaveRequest.EmployeeId
                                       && b.LeaveTypeId == leaveRequest.LeaveTypeId
                                       && b.Year == year);

            if (balance != null)
            {
                balance.UsedDays -= leaveRequest.TotalDays;
                balance.BalanceDays = balance.TotalDays + balance.CarryForwardDays - balance.UsedDays;
                balance.UpdatedAt = DateTime.UtcNow;
            }
        }

        leaveRequest.Status = "Cancelled";
        leaveRequest.CancelledOn = DateTime.UtcNow;
        leaveRequest.CancellationReason = reason;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Leave request {Id} cancelled", leaveRequestId);

        return leaveRequest;
    }

    /// <summary>
    /// Calculate leave days excluding weekends and public holidays
    /// Malaysia: Monday-Friday = working days
    /// </summary>
    public async Task<decimal> CalculateLeaveDaysAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            return 0;

        int month = startDate.Month;
        int year = startDate.Year;

        // Get public holidays
        var publicHolidays = await _timesheetService.GetPublicHolidaysInMonthAsync(month, year);
        var publicHolidayDates = publicHolidays.Select(d => d.Date).ToHashSet();

        // If end date is in different month, get those holidays too
        if (endDate.Month != startDate.Month || endDate.Year != startDate.Year)
        {
            var endMonthHolidays = await _timesheetService.GetPublicHolidaysInMonthAsync(endDate.Month, endDate.Year);
            foreach (var h in endMonthHolidays)
                publicHolidayDates.Add(h.Date);
        }

        decimal days = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Skip weekends
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            // Skip public holidays
            if (publicHolidayDates.Contains(date.Date))
                continue;

            days += 1;
        }

        return days;
    }

    /// <summary>
    /// Initialize leave balances for an employee for a specific year
    /// Creates balance records for all active leave types
    /// </summary>
    public async Task InitializeLeaveBalancesAsync(int employeeId, int year)
    {
        var employee = await _db.Employees.FindAsync(employeeId)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found");

        var activeLeaveTypes = await _db.LeaveTypes
            .Where(lt => lt.IsActive)
            .ToListAsync();

        foreach (var leaveType in activeLeaveTypes)
        {
            // Check if balance already exists
            var existing = await _db.EmployeeLeaveBalances
                .AnyAsync(b => b.EmployeeId == employeeId 
                            && b.LeaveTypeId == leaveType.Id 
                            && b.Year == year);

            if (existing)
                continue;

            var balance = new EmployeeLeaveBalance
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveType.Id,
                Year = year,
                TotalDays = leaveType.DefaultDaysPerYear,
                UsedDays = 0,
                BalanceDays = leaveType.DefaultDaysPerYear,
                CarryForwardDays = 0
            };

            _db.EmployeeLeaveBalances.Add(balance);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Leave balances initialized for Employee {EmployeeId} for year {Year}",
            employeeId, year);
    }

    /// <summary>
    /// Get all leave balances for an employee for a specific year
    /// </summary>
    public async Task<List<EmployeeLeaveBalance>> GetEmployeeLeaveBalancesAsync(int employeeId, int year)
    {
        return await _db.EmployeeLeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .ToListAsync();
    }
}
