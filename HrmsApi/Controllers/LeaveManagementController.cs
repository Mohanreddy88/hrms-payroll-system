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
    private readonly IEmailQueue _emailQueue;

    public LeaveManagementController(HrmsDbContext db, ILeaveService leaveService, IEmailService emailService, ILogger<LeaveManagementController> logger, IEmailQueue emailQueue)
    {
        _db = db;
        _leaveService = leaveService;
        _emailService = emailService;
        _logger = logger;
        _emailQueue = emailQueue;
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

            // Queue email — returns instantly, no 504 risk
            try
            {
                var body = await BuildLeaveApprovedEmailBody(
                    leaveRequest.Employee.Name, leaveRequest.LeaveType.Name,
                    leaveRequest.StartDate, leaveRequest.EndDate,
                    leaveRequest.TotalDays, leaveRequest.ApprovalRemarks ?? "");
                _emailQueue.Enqueue(new EmailJob
                {
                    ToEmail = leaveRequest.Employee.Email,
                    Subject = $"Leave Request Approved - {leaveRequest.LeaveType.Name}",
                    Body    = body
                });
                _logger.LogInformation("Leave approved email queued for {Email}, request {Id}", leaveRequest.Employee.Email, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue leave approved email for request {Id}", id);
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

            // Queue email — returns instantly, no 504 risk
            try
            {
                var employee  = await _db.Employees.FindAsync(leaveRequest.EmployeeId);
                var leaveType = await _db.LeaveTypes.FindAsync(leaveRequest.LeaveTypeId);
                if (employee != null && leaveType != null)
                {
                    var body = await BuildLeaveRejectedEmailBody(
                        employee.Name, leaveType.Name,
                        leaveRequest.StartDate, leaveRequest.EndDate,
                        leaveRequest.ApprovalRemarks ?? "");
                    _emailQueue.Enqueue(new EmailJob
                    {
                        ToEmail = employee.Email,
                        Subject = $"Leave Request Rejected - {leaveType.Name}",
                        Body    = body
                    });
                    _logger.LogInformation("Leave rejected email queued for {Email}, request {Id}", employee.Email, id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue leave rejected email for request {Id}", id);
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
    /// DELETE /api/leavemanagement/requests/{id} - Delete leave request
    /// Admin can delete any status; if Approved, restores the leave balance
    /// </summary>
    [HttpDelete("requests/{id:int}")]
    public async Task<IActionResult> DeleteLeaveRequest(int id)
    {
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");

        var request = await _db.LeaveRequests.FindAsync(id)
            ?? throw new KeyNotFoundException($"Leave request {id} not found");

        // Non-admin cannot delete approved requests
        if (request.Status == "Approved" && !isAdmin)
            return BadRequest(new { message = "Cannot delete approved leave request. Contact HR to correct it." });

        // If approved, restore the leave balance before deleting
        if (request.Status == "Approved")
        {
            var balance = await _db.EmployeeLeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId  == request.EmployeeId
                                       && b.LeaveTypeId == request.LeaveTypeId
                                       && b.Year        == request.StartDate.Year);
            if (balance != null)
            {
                balance.UsedDays    = Math.Max(0, balance.UsedDays - request.TotalDays);
                balance.BalanceDays = balance.TotalDays + balance.CarryForwardDays - balance.UsedDays;
                balance.UpdatedAt   = DateTime.UtcNow;
                _logger.LogInformation(
                    "Admin deleted approved leave {Id}: restored {Days} days to employee {EmpId} balance",
                    id, request.TotalDays, request.EmployeeId);
            }
        }

        _db.LeaveRequests.Remove(request);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Leave request deleted" + (request.Status == "Approved" ? " and leave balance restored" : "") });
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

    // ── Email body helpers (build HTML locally, no SMTP call) ────────────────
    private Task<string> BuildLeaveApprovedEmailBody(string name, string leaveType, DateTime start, DateTime end, decimal days, string remarks) =>
        Task.FromResult($@"<!DOCTYPE html><html><head><style>
          body{{font-family:Arial,sans-serif;color:#333;}}
          .header{{background:linear-gradient(135deg,#10b981,#059669);color:white;padding:28px;text-align:center;border-radius:8px 8px 0 0;}}
          .header h1{{margin:0;font-size:22px;}} .body{{background:#f8f9fa;padding:28px;border-radius:0 0 8px 8px;}}
          .grid{{background:white;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;margin:16px 0;}}
          .row{{display:flex;justify-content:space-between;padding:10px 16px;border-bottom:1px solid #f1f5f9;font-size:14px;}}
          .row:last-child{{border-bottom:none;}} .label{{color:#64748b;}} .value{{font-weight:600;color:#1e293b;}}
          .notice{{background:#f0fdf4;border:1px solid #bbf7d0;color:#166534;padding:10px 14px;border-radius:8px;font-size:13px;margin-top:12px;}}
          .footer{{text-align:center;font-size:12px;color:#888;margin-top:20px;}}
        </style></head><body>
          <div class=""header""><h1>✅ Leave Request Approved</h1></div>
          <div class=""body"">
            <p>Dear <strong>{name}</strong>, your leave request has been <strong>approved</strong>.</p>
            <div class=""grid"">
              <div class=""row""><span class=""label"">Leave Type</span><span class=""value"">{leaveType}</span></div>
              <div class=""row""><span class=""label"">Period</span><span class=""value"">{start:dd MMM yyyy} – {end:dd MMM yyyy}</span></div>
              <div class=""row""><span class=""label"">Total Days</span><span class=""value"">{days}</span></div>
              {(string.IsNullOrEmpty(remarks) ? "" : $"<div class=\"row\"><span class=\"label\">Remarks</span><span class=\"value\">{remarks}</span></div>")}
            </div>
            <div class=""notice"">✉️ Your leave balance has been updated. No further action required.</div>
          </div>
          <div class=""footer"">© {DateTime.Now.Year} HRMS. This is an automated email.</div>
        </body></html>");

    private Task<string> BuildLeaveRejectedEmailBody(string name, string leaveType, DateTime start, DateTime end, string remarks) =>
        Task.FromResult($@"<!DOCTYPE html><html><head><style>
          body{{font-family:Arial,sans-serif;color:#333;}}
          .header{{background:linear-gradient(135deg,#ef4444,#dc2626);color:white;padding:28px;text-align:center;border-radius:8px 8px 0 0;}}
          .header h1{{margin:0;font-size:22px;}} .body{{background:#f8f9fa;padding:28px;border-radius:0 0 8px 8px;}}
          .grid{{background:white;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;margin:16px 0;}}
          .row{{display:flex;justify-content:space-between;padding:10px 16px;border-bottom:1px solid #f1f5f9;font-size:14px;}}
          .row:last-child{{border-bottom:none;}} .label{{color:#64748b;}} .value{{font-weight:600;color:#1e293b;}}
          .notice{{background:#fff7ed;border:1px solid #fed7aa;color:#9a3412;padding:10px 14px;border-radius:8px;font-size:13px;margin-top:12px;}}
          .footer{{text-align:center;font-size:12px;color:#888;margin-top:20px;}}
        </style></head><body>
          <div class=""header""><h1>❌ Leave Request Rejected</h1></div>
          <div class=""body"">
            <p>Dear <strong>{name}</strong>, unfortunately your leave request has been <strong>rejected</strong>.</p>
            <div class=""grid"">
              <div class=""row""><span class=""label"">Leave Type</span><span class=""value"">{leaveType}</span></div>
              <div class=""row""><span class=""label"">Period</span><span class=""value"">{start:dd MMM yyyy} – {end:dd MMM yyyy}</span></div>
              {(string.IsNullOrEmpty(remarks) ? "" : $"<div class=\"row\"><span class=\"label\">Reason</span><span class=\"value\">{remarks}</span></div>")}
            </div>
            <div class=""notice"">Please contact HR if you have any questions.</div>
          </div>
          <div class=""footer"">© {DateTime.Now.Year} HRMS. This is an automated email.</div>
        </body></html>");
}

public class CancelRequest
{
    public string? Reason { get; set; }
}
