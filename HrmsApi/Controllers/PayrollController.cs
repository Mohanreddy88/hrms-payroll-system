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
    private readonly IAuditService _audit;
    private readonly INotificationService _notif;

    public PayrollController(
        HrmsDbContext db,
        IEmailService emailService,
        IPdfService pdfService,
        IPayrollCalculationService payrollService,
        IAuditService audit,
        INotificationService notif)
    {
        _db             = db;
        _emailService   = emailService;
        _pdfService     = pdfService;
        _payrollService = payrollService;
        _audit          = audit;
        _notif          = notif;
    }

    // Helper: find User.Id for an employee so we can send them a notification
    private async Task<int?> GetUserIdForEmployeeAsync(int employeeId)
    {
        var emp = await _db.Employees.FindAsync(employeeId);
        if (emp == null) return null;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emp.Email.ToLower());
        return user?.Id;
    }

    private string CurrentUser()   => User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
    private int    GetCurrentUserId() { var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; return int.TryParse(v, out var id) ? id : 0; }

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
                p.Remarks,
                p.IsProrated,
                p.ProratedDays,
                p.TotalMonthDays,
                p.ProratedSalary
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
            payroll.IsProrated,
            payroll.ProratedDays,
            payroll.TotalMonthDays,
            payroll.ProratedSalary,
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
                createdBy = a.Creator.Username
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
        try
        {
            var userId = GetCurrentUserId();
            var payroll = await _payrollService.GeneratePayrollAsync(request.EmployeeId, request.Month, request.Year, userId);
            await _audit.LogAsync(
                action:      "Generated",
                entityType:  "Payroll",
                entityId:    payroll.Id.ToString(),
                description: $"Generated payroll for employee {request.EmployeeId} — {new DateTime(request.Year, request.Month, 1):MMMM yyyy} (Net: {payroll.NetSalary:F2})",
                username:    CurrentUser(),
                userId:      userId,
                ipAddress:   HttpContext.Connection.RemoteIpAddress?.ToString());
            // Notify employee that their payslip has been generated
            var genEmpUserId = await GetUserIdForEmployeeAsync(request.EmployeeId);
            if (genEmpUserId.HasValue)
            {
                await _notif.NotifyUserAsync(
                    userId:  genEmpUserId.Value,
                    title:   "Payslip Generated",
                    message: $"Your payslip for {new DateTime(request.Year, request.Month, 1):MMMM yyyy} has been generated. Net salary: RM {payroll.NetSalary:F2}.",
                    type:    "payroll_generated",
                    link:    "/self-service/my-payslips");
            }
            return Ok(new
            {
                message = $"Payroll generated successfully for {new DateTime(request.Year, request.Month, 1):MMMM yyyy}",
                payrollId = payroll.Id,
                netSalary = payroll.NetSalary
            });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? "";
            return StatusCode(500, new
            {
                error = ex.Message,
                inner,
                stackTrace = ex.StackTrace
            });
        }
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
        await _audit.LogAsync(
            action:      "Approved",
            entityType:  "Payroll",
            entityId:    id.ToString(),
            description: $"Approved payroll #{id} for employee {payroll.EmployeeId} ({payroll.Month}/{payroll.Year})",
            username:    CurrentUser(),
            userId:      userId,
            newValues:   request?.Remarks,
            ipAddress:   HttpContext.Connection.RemoteIpAddress?.ToString());
        // Notify employee
        var appEmpUserId = await GetUserIdForEmployeeAsync(payroll.EmployeeId);
        if (appEmpUserId.HasValue)
        {
            await _notif.NotifyUserAsync(
                userId:  appEmpUserId.Value,
                title:   "Payslip Approved",
                message: $"Your payslip for {new DateTime(payroll.Year, payroll.Month, 1):MMMM yyyy} has been approved.",
                type:    "payroll_approved",
                link:    "/self-service/my-payslips");
        }
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
        await _audit.LogAsync(
            action:      "Rejected",
            entityType:  "Payroll",
            entityId:    id.ToString(),
            description: $"Rejected payroll #{id} for employee {payroll.EmployeeId} ({payroll.Month}/{payroll.Year}). Reason: {request.Reason}",
            username:    CurrentUser(),
            userId:      userId,
            newValues:   request.Reason,
            ipAddress:   HttpContext.Connection.RemoteIpAddress?.ToString());
        // Notify employee
        var rejEmpUserId = await GetUserIdForEmployeeAsync(payroll.EmployeeId);
        if (rejEmpUserId.HasValue)
        {
            await _notif.NotifyUserAsync(
                userId:  rejEmpUserId.Value,
                title:   "Payslip Rejected",
                message: $"Your payslip for {new DateTime(payroll.Year, payroll.Month, 1):MMMM yyyy} was rejected. Reason: {request.Reason}",
                type:    "payroll_rejected",
                link:    "/self-service/my-payslips");
        }
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
        await _audit.LogAsync(
            action:      "Processed",
            entityType:  "Payroll",
            entityId:    id.ToString(),
            description: $"Marked payroll #{id} as processed for employee {payroll.EmployeeId} ({payroll.Month}/{payroll.Year})",
            username:    CurrentUser(),
            userId:      userId,
            ipAddress:   HttpContext.Connection.RemoteIpAddress?.ToString());
        // Notify employee that salary has been paid/processed
        var procEmpUserId = await GetUserIdForEmployeeAsync(payroll.EmployeeId);
        if (procEmpUserId.HasValue)
        {
            await _notif.NotifyUserAsync(
                userId:  procEmpUserId.Value,
                title:   "Salary Processed",
                message: $"Your salary for {new DateTime(payroll.Year, payroll.Month, 1):MMMM yyyy} has been processed. Net: RM {payroll.NetSalary:F2}. Please check your bank account.",
                type:    "payroll_processed",
                link:    "/self-service/my-payslips");
        }
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

    /// <summary>DELETE /api/payroll/{id}/adjustment/{adjustmentId} — remove an adjustment</summary>
    [HttpDelete("{id:int}/adjustment/{adjustmentId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAdjustment(int id, int adjustmentId)
    {
        var payroll = await _db.Payrolls.FindAsync(id);
        if (payroll == null) return NotFound(new { message = $"Payroll {id} not found" });

        if (payroll.Status == "Processed")
            return BadRequest(new { message = "Cannot modify adjustments on a processed payroll." });

        var adjustment = await _db.PayrollAdjustments
            .FirstOrDefaultAsync(a => a.Id == adjustmentId && a.PayrollId == id);

        if (adjustment == null) return NotFound(new { message = "Adjustment not found." });

        // Reverse the effect on payroll totals
        if (adjustment.AdjustmentType == "Allowance" || adjustment.AdjustmentType == "Bonus")
        {
            payroll.Allowances  -= adjustment.Amount;
            payroll.GrossIncome -= adjustment.Amount;
        }
        else if (adjustment.AdjustmentType == "Deduction")
        {
            payroll.Deductions -= adjustment.Amount;
        }

        payroll.NetSalary = payroll.GrossIncome - payroll.EpfAmount - payroll.SocsoAmount - payroll.TaxAmount - payroll.Deductions;

        _db.PayrollAdjustments.Remove(adjustment);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Adjustment removed successfully.", newNetSalary = payroll.NetSalary });
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

        var empId  = record.EmployeeId;
        var period = $"{record.Month}/{record.Year}";
        _db.Payrolls.Remove(record);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(
            action:      "Deleted",
            entityType:  "Payroll",
            entityId:    id.ToString(),
            description: $"Deleted payroll #{id} for employee {empId} ({period})",
            username:    CurrentUser(),
            userId:      GetCurrentUserId(),
            ipAddress:   HttpContext.Connection.RemoteIpAddress?.ToString());
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

            var monthLabel = new DateTime(payroll.Year, payroll.Month, 1).ToString("MMMM yyyy");
            var subject    = $"Payslip for {monthLabel}";
            var body = $@"<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;line-height:1.8;color:#1a1a1a;background:#f4f4f4;margin:0;padding:0;}}
  .wrap{{max-width:580px;margin:30px auto;background:white;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);}}
  .header{{background:#1e40af;padding:24px 30px;}}
  .header h2{{margin:0;color:white;font-size:18px;font-weight:600;}}
  .header p{{margin:4px 0 0;color:#93c5fd;font-size:13px;}}
  .body{{padding:28px 30px;}}
  .body p{{margin:0 0 12px;font-size:14px;color:#374151;}}
  .notice{{background:#f0f9ff;border-left:4px solid #0ea5e9;padding:12px 16px;border-radius:4px;font-size:13px;color:#0369a1;margin-top:16px;}}
  .footer{{background:#f9fafb;padding:16px 30px;text-align:center;font-size:11px;color:#9ca3af;border-top:1px solid #f1f5f9;}}
</style></head><body>
  <div class=""wrap"">
    <div class=""header"">
      <h2>ESYNC SOFTWARE SERVICES SDN.BHD</h2>
      <p>Payslip Notification — {monthLabel}</p>
    </div>
    <div class=""body"">
      <p>Dear <strong>{payroll.Employee.Name}</strong>,</p>
      <p>Please find your payslip for <strong>{monthLabel}</strong> attached to this email.</p>
      <div class=""notice"">
        Your detailed payslip PDF is attached. Please login to the HRMS portal for full payroll details.
      </div>
    </div>
    <div class=""footer"">
      This is an auto-generated email. Please do not reply. &copy; {DateTime.Now.Year} ESYNC SOFTWARE SERVICES SDN.BHD
    </div>
  </div>
</body></html>";

            await _emailService.SendEmailWithAttachmentAsync(payroll.Employee.Email, subject, body, pdfData, fileName);

            return Ok(new { message = $"Payslip email with PDF sent to {payroll.Employee.Email}" });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? "";
            return StatusCode(500, new { message = $"Failed to send email: {ex.Message}", detail = inner });
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

    // Legacy helper — superseded by the inline method at the top of the class

    // ─────────────────────────────────────────────────────────────────────────────
    // BANK EXPORT
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/payroll/bank-export?month=5&year=2026&status=Approved
    /// Returns a CSV file ready for bulk bank payment upload.
    /// Columns: EmployeeCode, EmployeeName, BankName, AccountNumber, NetSalary, PaymentReference
    /// </summary>
    [HttpGet("bank-export")]
    public async Task<IActionResult> BankExport(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy");

        var payrolls = await _db.Payrolls
            .Include(p => p.Employee)
                .ThenInclude(e => e.Bank)
            .Where(p => p.Month == month && p.Year == year && p.Status == "Processed")
            .OrderBy(p => p.Employee.EmployeeCode)
            .ToListAsync();

        if (!payrolls.Any())
        {
            // Check if payrolls exist but are not yet Processed
            var pendingStatuses = await _db.Payrolls
                .Where(p => p.Month == month && p.Year == year)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var message = !pendingStatuses.Any()
                ? $"No payroll records found for {monthName}. Please generate payroll first."
                : $"No Processed payroll records found for {monthName}. " +
                  $"Current status: {string.Join(", ", pendingStatuses.Select(s => $"{s.Count} {s.Status}"))}. " +
                  $"Please approve and process the payroll before exporting for bank payment.";

            return Ok(new { hasRecords = false, message });
        }

        var monthLabel = new DateTime(year, month, 1).ToString("MMMM_yyyy");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("EmployeeCode,EmployeeName,BankName,AccountNumber,NetSalary,PaymentReference");

        foreach (var p in payrolls)
        {
            var bankName   = p.Employee.Bank?.Name ?? "";
            var accountNo  = p.Employee.AccountNumber ?? "";
            var reference  = $"SAL-{p.Employee.EmployeeCode}-{monthLabel}";
            // Escape any commas in name / bank name
            var empName    = $"\"{p.Employee.Name.Replace("\"", "\"\"")}\"";
            var bankEsc    = $"\"{bankName.Replace("\"", "\"\"")}\"";
            sb.AppendLine($"{p.Employee.EmployeeCode},{empName},{bankEsc},{accountNo},{p.NetSalary:F2},{reference}");
        }

        var bytes    = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BankPayment_{monthLabel}_Processed.csv";
        return File(bytes, "text/csv", fileName);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // YEAR-TO-DATE (YTD) SUMMARY
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/payroll/ytd/{employeeId}/{year}
    /// Returns cumulative payroll totals for the employee for the given year.
    /// </summary>
    [HttpGet("ytd/{employeeId:int}/{year:int}")]
    public async Task<IActionResult> GetYtd(int employeeId, int year)
    {
        var employee = await _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            return NotFound(new { message = "Employee not found." });

        var payrolls = await _db.Payrolls
            .Where(p => p.EmployeeId == employeeId && p.Year == year)
            .OrderBy(p => p.Month)
            .ToListAsync();

        var ytd = new
        {
            employeeId,
            employeeName     = employee.Name,
            employeeCode     = employee.EmployeeCode,
            designation      = employee.Designation,
            departmentName   = employee.Department?.Name ?? "",
            year,
            monthsProcessed  = payrolls.Count,
            ytdGrossIncome   = payrolls.Sum(p => p.GrossIncome),
            ytdBasicSalary   = payrolls.Sum(p => p.BasicSalary),
            ytdAllowances    = payrolls.Sum(p => p.Allowances),
            ytdDeductions    = payrolls.Sum(p => p.Deductions),
            ytdEpf           = payrolls.Sum(p => p.EpfAmount),
            ytdSocso         = payrolls.Sum(p => p.SocsoAmount),
            ytdTax           = payrolls.Sum(p => p.TaxAmount),
            ytdNetSalary     = payrolls.Sum(p => p.NetSalary),
            ytdPaidLeaveDays = payrolls.Sum(p => p.PaidLeaveDays ?? 0),
            ytdUnpaidLeaveDays = payrolls.Sum(p => p.UnpaidLeaveDays ?? 0),
            monthlyBreakdown = payrolls.Select(p => new
            {
                p.Id,
                p.Month,
                monthName    = new DateTime(p.Year, p.Month, 1).ToString("MMMM"),
                p.GrossIncome,
                p.BasicSalary,
                p.Allowances,
                p.Deductions,
                p.EpfAmount,
                p.SocsoAmount,
                p.TaxAmount,
                p.NetSalary,
                p.Status,
                p.IsProrated,
                p.ProratedDays,
                p.TotalMonthDays
            }).ToList()
        };

        return Ok(ytd);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // EA FORM / PCB II  (Malaysia Annual Income Statement)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/payroll/ea-form/{employeeId}/{year}
    /// Returns the EA Form data (Borang EA) for the employee for the given tax year.
    /// Covers: gross income, EPF, SOCSO, PCB, allowances, deductions, net taxable income.
    /// </summary>
    [HttpGet("ea-form/{employeeId:int}/{year:int}")]
    public async Task<IActionResult> GetEaForm(int employeeId, int year)
    {
        var employee = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Bank)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            return NotFound(new { message = "Employee not found." });

        // Only include Approved or Processed payrolls for the EA form
        var payrolls = await _db.Payrolls
            .Where(p => p.EmployeeId == employeeId
                     && p.Year == year
                     && (p.Status == "Approved" || p.Status == "Processed"))
            .OrderBy(p => p.Month)
            .ToListAsync();

        if (!payrolls.Any())
            return NotFound(new { message = $"No approved/processed payroll records found for {year}." });

        // EA Form Part B — Employment Income
        var totalGross        = payrolls.Sum(p => p.GrossIncome);
        var totalBasicSalary  = payrolls.Sum(p => p.BasicSalary);
        var totalAllowances   = payrolls.Sum(p => p.Allowances);
        var totalEpf          = payrolls.Sum(p => p.EpfAmount);
        var totalSocso        = payrolls.Sum(p => p.SocsoAmount);
        var totalTax          = payrolls.Sum(p => p.TaxAmount);       // PCB total
        var totalDeductions   = payrolls.Sum(p => p.Deductions);
        var totalNet          = payrolls.Sum(p => p.NetSalary);

        // Taxable income = Gross - EPF (statutory relief already applied in PCB calc)
        var taxableIncome = totalGross - totalEpf;

        var eaForm = new
        {
            // Header
            formTitle        = $"EA FORM (BORANG EA) — Year of Assessment {year}",
            employerId       = "ESYNC SOFTWARE SERVICES SDN.BHD",
            // Employee Details (Part A)
            employeeId,
            employeeName     = employee.Name,
            employeeCode     = employee.EmployeeCode,
            icPassport       = employee.IcPassport,
            taxNumber        = employee.TaxNumber,
            designation      = employee.Designation,
            departmentName   = employee.Department?.Name ?? "",
            joinDate         = employee.JoinDate.ToString("dd MMM yyyy"),
            gender           = employee.Gender,
            // Employment Income (Part B)
            yearOfAssessment = year,
            monthsCovered    = payrolls.Count,
            totalBasicSalary,
            totalAllowances,
            totalGrossIncome = totalGross,
            // Statutory Deductions (Part C)
            totalEpfEmployee = totalEpf,
            totalSocsoEmployee = totalSocso,
            totalPcbMtd      = totalTax,       // PCB/MTD withheld
            totalOtherDeductions = totalDeductions,
            // Taxable Income
            taxableIncome,
            totalNetSalary   = totalNet,
            // Monthly breakdown for verification
            monthlyBreakdown = payrolls.Select(p => new
            {
                p.Month,
                monthName   = new DateTime(p.Year, p.Month, 1).ToString("MMMM"),
                p.BasicSalary,
                p.Allowances,
                p.GrossIncome,
                p.EpfAmount,
                p.SocsoAmount,
                p.TaxAmount,
                p.Deductions,
                p.NetSalary,
                p.Status,
                p.IsProrated
            }).ToList(),
            generatedOn = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm") + " UTC"
        };

        return Ok(eaForm);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // YEAR-END SUMMARY — list all employees with payroll for the year
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/payroll/year-end-summary/{year}
    /// Returns all employees who have payroll records for the given year,
    /// with their totals and EA Form readiness status.
    /// </summary>
    [HttpGet("year-end-summary/{year:int}")]
    public async Task<IActionResult> GetYearEndSummary(int year)
    {
        var records = await _db.Payrolls
            .Include(p => p.Employee).ThenInclude(e => e.Department)
            .Where(p => p.Year == year)
            .ToListAsync();

        if (!records.Any())
            return Ok(new { hasRecords = false, message = $"No payroll records found for {year}.", employees = new List<object>() });

        var grouped = records
            .GroupBy(p => p.EmployeeId)
            .Select(g =>
            {
                var emp         = g.First().Employee;
                var allMonths   = g.OrderBy(p => p.Month).ToList();
                var processed   = allMonths.Where(p => p.Status == "Processed" || p.Status == "Approved").ToList();
                var monthsDone  = processed.Count;
                var isEaReady   = monthsDone > 0;

                return new
                {
                    employeeId      = emp.Id,
                    employeeName    = emp.Name,
                    employeeCode    = emp.EmployeeCode,
                    email           = emp.Email,
                    designation     = emp.Designation,
                    departmentName  = emp.Department?.Name ?? "",
                    monthsProcessed = monthsDone,
                    totalMonths     = allMonths.Count,
                    ytdGross        = processed.Sum(p => p.GrossIncome),
                    ytdBasic        = processed.Sum(p => p.BasicSalary),
                    ytdAllowances   = processed.Sum(p => p.Allowances),
                    ytdEpf          = processed.Sum(p => p.EpfAmount),
                    ytdSocso        = processed.Sum(p => p.SocsoAmount),
                    ytdTax          = processed.Sum(p => p.TaxAmount),
                    ytdNet          = processed.Sum(p => p.NetSalary),
                    isEaReady,
                    hasEmail        = !string.IsNullOrWhiteSpace(emp.Email)
                };
            })
            .OrderBy(e => e.employeeCode)
            .ToList();

        return Ok(new { hasRecords = true, year, employees = grouped });
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // EMAIL EA FORM TO EMPLOYEE
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/payroll/email-ea-form/{employeeId}/{year}
    /// Generates the EA Form summary and emails it to the employee.
    /// </summary>
    [HttpPost("email-ea-form/{employeeId:int}/{year:int}")]
    public async Task<IActionResult> EmailEaForm(int employeeId, int year)
    {
        var employee = await _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            return NotFound(new { message = "Employee not found." });

        if (string.IsNullOrWhiteSpace(employee.Email))
            return BadRequest(new { message = "Employee does not have an email address configured." });

        var payrolls = await _db.Payrolls
            .Where(p => p.EmployeeId == employeeId
                     && p.Year == year
                     && (p.Status == "Approved" || p.Status == "Processed"))
            .OrderBy(p => p.Month)
            .ToListAsync();

        if (!payrolls.Any())
            return NotFound(new { message = $"No approved/processed payroll records found for {year}. EA Form cannot be generated." });

        // Generate CP8A PDF
        var pdfBytes = await _pdfService.GenerateEaFormPdfAsync(employeeId, year);
        var firstName = employee.Name.Split(' ').FirstOrDefault() ?? employee.Name;
        var attachmentName = $"EA_Form_{employee.EmployeeCode}_{year}.pdf";

        var subject = $"EA Form for Year of Assessment {year}";

        var body = $@"<html><body style=""font-family:Arial,sans-serif;font-size:14px;color:#222;"">
<p>Dear {firstName},</p>
<p>Greetings from HR.</p>
<p>Please find attached your <strong>EA Form for the Year of Assessment {year}</strong> for your reference and income tax filing purposes.</p>
<p>Kindly review the details and let us know if you require any clarification.</p>
<p>Thank you and wish you a smooth tax filing process.</p>
<br/>
<p><strong>HR Department</strong><br/>
Esync Software Services<br/>
Tel: +603 8322 8593<br/>
Website: <a href=""https://www.esyncsoftware.my"">www.esyncsoftware.my</a> | Global Business Services</p>
<hr style=""border:none;border-top:1px solid #ddd;margin:16px 0;""/>
<p style=""font-size:11px;color:#888;""><em>This email is confidential and is intended only for the individual or entity to which it is addressed.</em></p>
</body></html>";

        try
        {
            await _emailService.SendEmailWithAttachmentAsync(
                employee.Email, subject, body, pdfBytes, attachmentName);

            await _audit.LogAsync("EMAIL_EA_FORM", "Payroll",
                employeeId.ToString(), $"EA Form PDF for {year} emailed to {employee.Email}",
                User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "system");

            return Ok(new { message = $"EA Form for {year} successfully emailed to {employee.Email}." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to send EA Form email: {ex.Message}" });
        }
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
