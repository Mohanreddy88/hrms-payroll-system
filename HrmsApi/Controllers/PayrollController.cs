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
public class PayrollController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IPdfService _pdfService;
    private readonly IPayrollCalculationService _payrollService;

    public PayrollController(
        HrmsDbContext db, 
        IEmailService emailService, 
        IPdfService pdfService,
        IPayrollCalculationService payrollService)
    {
        _db = db;
        _emailService = emailService;
        _pdfService = pdfService;
        _payrollService = payrollService;
    }

    /// <summary>GET /api/payroll?status=Draft&month=5&year=2026 — get all payroll records with filters</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        var query = _db.Payrolls
            .Include(p => p.Employee)
            .Include(p => p.Approver)
            .Include(p => p.Processor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (month.HasValue)
        {
            query = query.Where(p => p.Month == month.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(p => p.Year == year.Value);
        }

        var records = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ThenByDescending(p => p.GeneratedOn)
            .Select(p => new
            {
                p.Id,
                p.EmployeeId,
                employeeCode = p.Employee.EmployeeCode,
                employeeName = p.Employee.Name,
                p.Month,
                p.Year,
                monthYear = new DateTime(p.Year, p.Month, 1).ToString("MMMM yyyy"),
                p.BasicSalary,
                p.Allowances,
                p.Deductions,
                p.EpfAmount,
                p.SocsoAmount,
                p.TaxAmount,
                p.GrossIncome,
                p.NetSalary,
                p.Status,
                p.AttendanceHours,
                p.ExpectedHours,
                p.PaidLeaveDays,
                p.UnpaidLeaveDays,
                p.GeneratedOn,
                p.ApprovedBy,
                approverName = p.Approver != null ? p.Approver.Username : null,
                p.ApprovedOn,
                p.ProcessedBy,
                processorName = p.Processor != null ? p.Processor.Username : null,
                p.ProcessedOn,
                p.Remarks
            })
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>GET /api/payroll/{id} — get payroll by ID with full details</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payroll = await _db.Payrolls
            .Include(p => p.Employee)
            .Include(p => p.Approver)
            .Include(p => p.Processor)
            .Include(p => p.AttendancePeriods)
                .ThenInclude(ap => ap.AttendancePeriod)
            .Include(p => p.LeaveRequests)
                .ThenInclude(lr => lr.LeaveRequest)
                    .ThenInclude(r => r.LeaveType)
            .Include(p => p.Adjustments)
                .ThenInclude(a => a.Creator)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payroll == null)
        {
            return NotFound(new { message = $"Payroll record {id} not found" });
        }

        return Ok(new
        {
            payroll.Id,
            payroll.EmployeeId,
            employeeCode = payroll.Employee.EmployeeCode,
            employeeName = payroll.Employee.Name,
            payroll.Month,
            payroll.Year,
            monthYear = new DateTime(payroll.Year, payroll.Month, 1).ToString("MMMM yyyy"),
            payroll.BasicSalary,
            payroll.Allowances,
            payroll.Deductions,
            payroll.EpfAmount,
            payroll.SocsoAmount,
            payroll.TaxAmount,
            payroll.GrossIncome,
            payroll.NetSalary,
            payroll.Status,
            payroll.AttendanceHours,
            payroll.ExpectedHours,
            payroll.PaidLeaveDays,
            payroll.UnpaidLeaveDays,
            payroll.GeneratedOn,
            payroll.ApprovedBy,
            approverName = payroll.Approver?.Username,
            payroll.ApprovedOn,
            payroll.ProcessedBy,
            processorName = payroll.Processor?.Username,
            payroll.ProcessedOn,
            payroll.Remarks,
            attendancePeriods = payroll.AttendancePeriods.Select(ap => new
            {
                ap.Id,
                ap.AttendancePeriodId,
                startDate = ap.AttendancePeriod.StartDate,
                endDate = ap.AttendancePeriod.EndDate,
                ap.HoursWorked,
                status = ap.AttendancePeriod.Status
            }),
            leaveRequests = payroll.LeaveRequests.Select(lr => new
            {
                lr.Id,
                lr.LeaveRequestId,
                leaveType = lr.LeaveRequest.LeaveType.Name,
                leaveTypeCode = lr.LeaveRequest.LeaveType.Code,
                startDate = lr.LeaveRequest.StartDate,
                endDate = lr.LeaveRequest.EndDate,
                lr.LeaveDays,
                lr.IsPaid,
                lr.DeductionAmount
            }),
            adjustments = payroll.Adjustments.Select(a => new
            {
                a.Id,
                a.AdjustmentType,
                a.Description,
                a.Amount,
                createdBy = a.Creator.Username,
                a.CreatedAt
            })
        });
    }

    /// <summary>GET /api/payroll/employee/{employeeId} — payroll by employee</summary>
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var records = await _db.Payrolls
            .Include(p => p.Employee)
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new
            {
                p.Id,
                p.Month,
                p.Year,
                monthYear = new DateTime(p.Year, p.Month, 1).ToString("MMMM yyyy"),
                p.BasicSalary,
                p.GrossIncome,
                p.NetSalary,
                p.Status,
                p.GeneratedOn,
                p.ApprovedOn,
                p.ProcessedOn
            })
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>GET /api/payroll/eligibility/{employeeId}/{month}/{year} — check eligibility</summary>
    [HttpGet("eligibility/{employeeId:int}/{month:int}/{year:int}")]
    public async Task<IActionResult> CheckEligibility(int employeeId, int month, int year)
    {
        var result = await _payrollService.CheckEligibilityAsync(employeeId, month, year);
        return Ok(result);
    }

    /// <summary>POST /api/payroll/calculate — preview payroll calculation (does not save)</summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> CalculatePayroll([FromBody] PayrollCalculateRequest request)
    {
        var result = await _payrollService.CalculatePayrollAsync(request.EmployeeId, request.Month, request.Year);
        return Ok(result);
    }

    /// <summary>POST /api/payroll/generate — generate payroll for single employee</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateSingle([FromBody] PayrollGenerateRequest request)
    {
        var userId = GetCurrentUserId();
        var payroll = await _payrollService.GeneratePayrollAsync(request.EmployeeId, request.Month, request.Year, userId);
        return Ok(new
        {
            message = $"Payroll generated successfully for {new DateTime(request.Year, request.Month, 1):MMMM yyyy}",
            payrollId = payroll.Id,
            netSalary = payroll.NetSalary
        });
    }

    /// <summary>POST /api/payroll/generate-bulk — generate payroll for multiple employees</summary>
    [HttpPost("generate-bulk")]
    public async Task<IActionResult> GenerateBulk([FromBody] PayrollBulkGenerateRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _payrollService.GenerateBulkPayrollAsync(request.EmployeeIds, request.Month, request.Year, userId);
        
        return Ok(new
        {
            message = $"Bulk payroll generation completed: {result.SuccessCount} succeeded, {result.FailureCount} failed",
            successCount = result.SuccessCount,
            failureCount = result.FailureCount,
            errors = result.Errors,
            generatedPayrollIds = result.GeneratedPayrollIds
        });
    }

    /// <summary>POST /api/payroll/{id}/approve — approve payroll</summary>
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] PayrollApprovalRequest? request = null)
    {
        var userId = GetCurrentUserId();
        var payroll = await _payrollService.ApprovePayrollAsync(id, userId, request?.Remarks);
        
        return Ok(new
        {
            message = "Payroll approved successfully",
            payrollId = payroll.Id,
            status = payroll.Status,
            approvedOn = payroll.ApprovedOn
        });
    }

    /// <summary>POST /api/payroll/{id}/reject — reject payroll</summary>
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] PayrollRejectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "Rejection reason is required" });
        }

        var userId = GetCurrentUserId();
        var payroll = await _payrollService.RejectPayrollAsync(id, userId, request.Reason);
        
        return Ok(new
        {
            message = "Payroll rejected successfully",
            payrollId = payroll.Id,
            status = payroll.Status
        });
    }

    /// <summary>POST /api/payroll/{id}/process — mark as processed (payment completed)</summary>
    [HttpPost("{id:int}/process")]
    public async Task<IActionResult> Process(int id)
    {
        var userId = GetCurrentUserId();
        var payroll = await _payrollService.ProcessPayrollAsync(id, userId);
        
        return Ok(new
        {
            message = "Payroll marked as processed",
            payrollId = payroll.Id,
            status = payroll.Status,
            processedOn = payroll.ProcessedOn
        });
    }

    /// <summary>POST /api/payroll/{id}/adjustment — add manual adjustment</summary>
    [HttpPost("{id:int}/adjustment")]
    public async Task<IActionResult> AddAdjustment(int id, [FromBody] PayrollAdjustmentRequest request)
    {
        var payroll = await _db.Payrolls.FindAsync(id);
        if (payroll == null)
        {
            return NotFound(new { message = $"Payroll {id} not found" });
        }

        if (payroll.Status == "Processed")
        {
            return BadRequest(new { message = "Cannot add adjustments to processed payroll" });
        }

        var userId = GetCurrentUserId();
        var adjustment = new PayrollAdjustment
        {
            PayrollId = id,
            AdjustmentType = request.AdjustmentType,
            Description = request.Description,
            Amount = request.Amount,
            CreatedBy = userId
        };

        _db.PayrollAdjustments.Add(adjustment);

        // Update payroll totals
        if (request.AdjustmentType == "Allowance" || request.AdjustmentType == "Bonus")
        {
            payroll.Allowances += request.Amount;
            payroll.GrossIncome += request.Amount;
        }
        else if (request.AdjustmentType == "Deduction")
        {
            payroll.Deductions += request.Amount;
        }

        // Recalculate net salary
        payroll.NetSalary = payroll.GrossIncome - payroll.EpfAmount - payroll.SocsoAmount - payroll.TaxAmount - payroll.Deductions;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Adjustment added successfully",
            adjustmentId = adjustment.Id,
            newNetSalary = payroll.NetSalary
        });
    }

    /// <summary>DELETE /api/payroll/{id} — delete payroll record</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.Payrolls.FindAsync(id)
            ?? throw new KeyNotFoundException($"Payroll record {id} not found.");

        if (record.Status == "Processed")
        {
            return BadRequest(new { message = "Cannot delete processed payroll" });
        }

        _db.Payrolls.Remove(record);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>POST /api/payroll/email/{id} — send payslip email to employee</summary>
    [HttpPost("email/{id:int}")]
    public async Task<IActionResult> EmailPayslip(int id)
    {
        var payroll = await _db.Payrolls
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Payroll record {id} not found.");

        if (string.IsNullOrWhiteSpace(payroll.Employee.Email))
            return BadRequest(new { message = "Employee does not have an email address configured." });

        try
        {
            // Generate PDF attachment
            var pdfData = await _pdfService.GeneratePayslipPdfAsync(id);
            var monthName = new DateTime(payroll.Year, payroll.Month, 1).ToString("MMMM_yyyy");
            var fileName = $"Payslip_{payroll.Employee.Name.Replace(" ", "_")}_{monthName}.pdf";

            var subject = $"Payslip for {new DateTime(payroll.Year, payroll.Month, 1):MMMM yyyy}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #1a1a1a; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; }}
        .header {{ background: #4169E1; color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: normal; }}
        .content {{ padding: 40px 30px; background-color: white; }}
        .content p {{ margin: 10px 0; color: #1a1a1a; font-size: 15px; }}
        .salary-box {{ background: white; padding: 30px; margin: 30px 0; text-align: center; border: 3px solid #e0e0e0; border-left: 6px solid #10b981; }}
        .salary-label {{ margin: 0; font-size: 16px; color: #666; }}
        .salary-amount {{ font-size: 48px; font-weight: bold; color: #10b981; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🧾 Payslip Available</h1>
        </div>
        <div class=""content"">
            <p>Dear <strong>{payroll.Employee.Name}</strong>,</p>
            <p>Your payslip for <strong>{new DateTime(payroll.Year, payroll.Month, 1):MMMM yyyy}</strong> is now available.</p>
            
            <div class=""salary-box"">
                <p class=""salary-label"">Net Salary</p>
                <div class=""salary-amount"">RM {payroll.NetSalary:N2}</div>
            </div>

            <p>Please find your detailed payslip attached as a PDF document.</p>
        </div>
    </div>
</body>
</html>";

            await _emailService.SendEmailWithAttachmentAsync(payroll.Employee.Email, subject, body, pdfData, fileName);

            return Ok(new { message = $"Payslip email with PDF sent to {payroll.Employee.Email}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to send email: {ex.Message}" });
        }
    }

    /// <summary>POST /api/payroll/email-bulk — send payslips to multiple employees</summary>
    [HttpPost("email-bulk")]
    public async Task<IActionResult> EmailPayslipsBulk([FromBody] List<int> payrollIds)
    {
        var payrolls = await _db.Payrolls
            .Include(p => p.Employee)
            .Where(p => payrollIds.Contains(p.Id))
            .ToListAsync();

        var recipients = payrolls
            .Where(p => !string.IsNullOrWhiteSpace(p.Employee.Email))
            .Select(p => new PayslipEmailData
            {
                Email = p.Employee.Email,
                EmployeeName = p.Employee.Name,
                Month = p.Month.ToString(),
                Year = p.Year.ToString(),
                NetSalary = p.NetSalary,
                PayslipId = p.Id
            })
            .ToList();

        if (recipients.Count == 0)
            return BadRequest(new { message = "No valid email addresses found for the selected payslips." });

        try
        {
            await _emailService.SendBulkPayslipEmailsAsync(recipients);
            return Ok(new
            {
                message = $"Payslip emails sent to {recipients.Count} employees",
                sentCount = recipients.Count,
                skippedCount = payrollIds.Count - recipients.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to send emails: {ex.Message}" });
        }
    }

    // Helper method to get current user ID from JWT
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

// ── Request/Response DTOs ──────────────────────────────────

public class PayrollRequest
{
    public int EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
}

public class PayrollCalculateRequest
{
    public int EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class PayrollGenerateRequest
{
    public int EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class PayrollBulkGenerateRequest
{
    public List<int> EmployeeIds { get; set; } = new();
    public int Month { get; set; }
    public int Year { get; set; }
}

public class PayrollApprovalRequest
{
    public string? Remarks { get; set; }
}

public class PayrollRejectionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class PayrollAdjustmentRequest
{
    public string AdjustmentType { get; set; } = string.Empty; // Allowance, Deduction, Bonus, Overtime
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
