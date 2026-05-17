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
[Authorize]
public class LeaveManagementController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly ILeaveService _leaveService;
    private readonly IEmailService _emailService;
    private readonly ILogger<LeaveManagementController> _logger;

    public LeaveManagementController(HrmsDbContext db, ILeaveService leaveService, IEmailService emailService, ILogger<LeaveManagementController> logger)
    {
        _db = db;
        _leaveService = leaveService;
        _emailService = emailService;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // LEAVE TYPES
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/leavemanagement/leave-types - Get all leave types
    /// </summary>
    [HttpGet("leave-types")]
    public async Task<IActionResult> GetLeaveTypes()
    {
        var leaveTypes = await _db.LeaveTypes
            .OrderBy(lt => lt.Name)
            .ToListAsync();
        
        return Ok(leaveTypes);
    }

    /// <summary>
    /// GET /api/leavemanagement/leave-types/active - Get active leave types
    /// </summary>
    [HttpGet("leave-types/active")]
    public async Task<IActionResult> GetActiveLeaveTypes()
    {
        var leaveTypes = await _db.LeaveTypes
            .Where(lt => lt.IsActive)
            .OrderBy(lt => lt.Name)
            .ToListAsync();
        
        return Ok(leaveTypes);
    }

    /// <summary>
    /// POST /api/leavemanagement/leave-types - Create new leave type
    /// </summary>
    [HttpPost("leave-types")]
    public async Task<IActionResult> CreateLeaveType([FromBody] LeaveTypeRequest request)
    {
        var leaveType = new LeaveType
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            DefaultDaysPerYear = request.DefaultDaysPerYear,
            IsActive = request.IsActive,
            RequiresApproval = request.RequiresApproval,
            IsPaid = request.IsPaid
        };

        _db.LeaveTypes.Add(leaveType);
        await _db.SaveChangesAsync();

        return Ok(leaveType);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // LEAVE BALANCES
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/leavemanagement/balances/employee/{employeeId}?year=2026
    /// Get leave balances for an employee
    /// </summary>
    [HttpGet("balances/employee/{employeeId:int}")]
    public async Task<IActionResult> GetEmployeeLeaveBalances(int employeeId, [FromQuery] int? year)
    {
        var currentYear = year ?? DateTime.Now.Year;
        
        var balances = await _leaveService.GetEmployeeLeaveBalancesAsync(employeeId, currentYear);

        if (balances.Count == 0)
        {
            // Initialize if not exists
            await _leaveService.InitializeLeaveBalancesAsync(employeeId, currentYear);
            balances = await _leaveService.GetEmployeeLeaveBalancesAsync(employeeId, currentYear);
        }

        var result = balances.Select(b => new
        {
            b.Id,
            b.EmployeeId,
            b.LeaveTypeId,
            leaveTypeName = b.LeaveType.Name,
            leaveTypeCode = b.LeaveType.Code,
            b.Year,
            b.TotalDays,
            b.UsedDays,
            b.BalanceDays,
            b.CarryForwardDays,
            b.UpdatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// POST /api/leavemanagement/balances/initialize
    /// Initialize leave balances for an employee for a year
    /// </summary>
    [HttpPost("balances/initialize")]
    public async Task<IActionResult> InitializeLeaveBalances([FromBody] LeaveBalanceInit request)
    {
        await _leaveService.InitializeLeaveBalancesAsync(request.EmployeeId, request.Year);
        
        var balances = await _leaveService.GetEmployeeLeaveBalancesAsync(request.EmployeeId, request.Year);
        
        return Ok(new
        {
            message = $"Leave balances initialized for employee {request.EmployeeId} for year {request.Year}",
            balances
        });
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // LEAVE REQUESTS
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/leavemanagement/requests - Get all leave requests
    /// </summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetAllLeaveRequests(
        [FromQuery] string? status,
        [FromQuery] int? employeeId)
    {
        var query = _db.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(lr => lr.Status == status);

        if (employeeId.HasValue)
            query = query.Where(lr => lr.EmployeeId == employeeId.Value);

        var requests = await query
            .OrderByDescending(lr => lr.RequestedOn)
            .Select(lr => new
            {
                lr.Id,
                lr.EmployeeId,
                employeeName = lr.Employee.Name,
                lr.LeaveTypeId,
                leaveTypeName = lr.LeaveType.Name,
                leaveTypeCode = lr.LeaveType.Code,
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

        return Ok(requests);
    }

    /// <summary>
    /// GET /api/leavemanagement/requests/employee/{employeeId}
    /// Get leave requests for an employee
    /// </summary>
    [HttpGet("requests/employee/{employeeId:int}")]
    public async Task<IActionResult> GetEmployeeLeaveRequests(int employeeId, [FromQuery] int? year)
    {
        var query = _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == employeeId);

        if (year.HasValue)
            query = query.Where(lr => lr.StartDate.Year == year.Value);

        var requests = await query
            .OrderByDescending(lr => lr.RequestedOn)
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// GET /api/leavemanagement/requests/pending - Get pending leave requests
    /// </summary>
    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPendingLeaveRequests()
    {
        var requests = await _db.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.Status == "Pending")
            .OrderBy(lr => lr.StartDate)
            .Select(lr => new
            {
                lr.Id,
                lr.EmployeeId,
                employeeName = lr.Employee.Name,
                leaveTypeName = lr.LeaveType.Name,
                lr.StartDate,
                lr.EndDate,
                lr.TotalDays,
                lr.Reason,
                lr.RequestedOn
            })
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// POST /api/leavemanagement/requests - Create leave request
    /// </summary>
    [HttpPost("requests")]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] LeaveRequestCreate request)
    {
        try
        {
            var leaveRequest = await _leaveService.CreateLeaveRequestAsync(request);
            
            // Return DTO to avoid circular reference
            var result = new
            {
                leaveRequest.Id,
                leaveRequest.EmployeeId,
                leaveRequest.LeaveTypeId,
                leaveRequest.StartDate,
                leaveRequest.EndDate,
                leaveRequest.TotalDays,
                leaveRequest.Reason,
                leaveRequest.Status,
                leaveRequest.RequestedOn
            };
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/leavemanagement/requests/{id}/approve - Approve leave request
    /// </summary>
    [HttpPut("requests/{id:int}/approve")]
    public async Task<IActionResult> ApproveLeaveRequest(int id, [FromBody] LeaveRequestApproval approval)
    {
        try
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            var currentUserId = adminUser?.Id ?? 1;

            var leaveRequest = await _leaveService.ApproveLeaveRequestAsync(
                id,
                currentUserId,
                approval.ApprovalRemarks ?? ""
            );

            // Send email directly — scoped service is still alive during request
            try
            {
                await _emailService.SendLeaveApprovedEmailAsync(
                    leaveRequest.Employee.Email, leaveRequest.Employee.Name,
                    leaveRequest.LeaveType.Name, leaveRequest.StartDate,
                    leaveRequest.EndDate, leaveRequest.TotalDays,
                    leaveRequest.ApprovalRemarks ?? "");
                _logger.LogInformation("Leave approved email sent to {Email} for request {Id}", leaveRequest.Employee.Email, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave approved email for request {Id}", id);
            }

            return Ok(new
            {
                leaveRequest.Id,
                leaveRequest.EmployeeId,
                leaveRequest.LeaveTypeId,
                leaveRequest.Status,
                leaveRequest.ApprovedBy,
                leaveRequest.ApprovedOn,
                leaveRequest.ApprovalRemarks,
                message = "Leave request approved and employee notified by email"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/leavemanagement/requests/{id}/reject - Reject leave request
    /// </summary>
    [HttpPut("requests/{id:int}/reject")]
    public async Task<IActionResult> RejectLeaveRequest(int id, [FromBody] LeaveRequestApproval approval)
    {
        try
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            var currentUserId = adminUser?.Id ?? 1;

            var leaveRequest = await _leaveService.RejectLeaveRequestAsync(
                id,
                currentUserId,
                approval.ApprovalRemarks ?? ""
            );

            // Send email directly — scoped service is still alive during request
            try
            {
                var employee = await _db.Employees.FindAsync(leaveRequest.EmployeeId);
                var leaveType = await _db.LeaveTypes.FindAsync(leaveRequest.LeaveTypeId);
                if (employee != null && leaveType != null)
                {
                    await _emailService.SendLeaveRejectedEmailAsync(
                        employee.Email, employee.Name, leaveType.Name,
                        leaveRequest.StartDate, leaveRequest.EndDate,
                        leaveRequest.ApprovalRemarks ?? "");
                    _logger.LogInformation("Leave rejected email sent to {Email} for request {Id}", employee.Email, id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave rejected email for request {Id}", id);
            }

            return Ok(new
            {
                leaveRequest.Id,
                leaveRequest.EmployeeId,
                leaveRequest.LeaveTypeId,
                leaveRequest.Status,
                leaveRequest.ApprovedBy,
                leaveRequest.ApprovedOn,
                leaveRequest.ApprovalRemarks,
                message = "Leave request rejected and employee notified by email"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/leavemanagement/requests/{id}/cancel - Cancel leave request
    /// </summary>
    [HttpPut("requests/{id:int}/cancel")]
    public async Task<IActionResult> CancelLeaveRequest(int id, [FromBody] CancelRequest cancel)
    {
        try
        {
            var leaveRequest = await _leaveService.CancelLeaveRequestAsync(id, cancel.Reason ?? "");
            
            // Return DTO to avoid circular reference
            var result = new
            {
                leaveRequest.Id,
                leaveRequest.EmployeeId,
                leaveRequest.LeaveTypeId,
                leaveRequest.Status,
                leaveRequest.CancelledOn,
                leaveRequest.CancellationReason
            };
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/leavemanagement/requests/{id} - Delete leave request (only pending/rejected)
    /// </summary>
    [HttpDelete("requests/{id:int}")]
    public async Task<IActionResult> DeleteLeaveRequest(int id)
    {
        var request = await _db.LeaveRequests.FindAsync(id)
            ?? throw new KeyNotFoundException($"Leave request {id} not found");

        if (request.Status == "Approved")
            return BadRequest(new { message = "Cannot delete approved leave request. Cancel it instead." });

        _db.LeaveRequests.Remove(request);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// GET /api/leavemanagement/calculate-days?startDate=2026-05-01&endDate=2026-05-10
    /// Calculate leave days (excluding weekends and holidays)
    /// </summary>
    [HttpGet("calculate-days")]
    public async Task<IActionResult> CalculateLeaveDays(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var days = await _leaveService.CalculateLeaveDaysAsync(startDate, endDate);
        
        return Ok(new
        {
            startDate,
            endDate,
            totalDays = days,
            note = "Excluding weekends and public holidays"
        });
    }
}

public class CancelRequest
{
    public string? Reason { get; set; }
}
