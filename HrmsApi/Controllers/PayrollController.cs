using HrmsApi.Data;
using HrmsApi.Models;
using HrmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IPdfService _pdfService;

    public PayrollController(HrmsDbContext db, IEmailService emailService, IPdfService pdfService)
    {
        _db = db;
        _emailService = emailService;
        _pdfService = pdfService;
    }

    /// <summary>GET /api/payroll — get all payroll records</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var records = await _db.Payrolls
            .Include(p => p.Employee)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new
            {
                p.Id, p.EmployeeId,
                EmployeeName = p.Employee.Name,
                p.Month, p.Year,
                p.BasicSalary, p.Allowances, p.Deductions,
                p.EpfAmount, p.SocsoAmount, p.TaxAmount,
                p.GrossIncome, p.NetSalary,
                p.GeneratedOn
            })
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>GET /api/payroll/employee/{employeeId} — payroll by employee</summary>
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var records = await _db.Payrolls
            .Include(p => p.Employee)
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>POST /api/payroll — generate payroll</summary>
    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] PayrollRequest req)
    {
        var empExists = await _db.Employees.AnyAsync(e => e.Id == req.EmployeeId);
        if (!empExists) throw new KeyNotFoundException($"Employee {req.EmployeeId} not found.");

        var duplicate = await _db.Payrolls
            .AnyAsync(p => p.EmployeeId == req.EmployeeId && p.Month == req.Month && p.Year == req.Year);

        if (duplicate)
            throw new InvalidOperationException($"Payroll already generated for this employee for {req.Month}/{req.Year}.");

        // Statutory calculation constants
        const decimal epfRate   = 0.02m;    // 2% employee EPF
        const decimal socsoRate = 0.005m;   // 0.5% SOCSO
        const decimal taxRate   = 0.1197m;  // 11.97% income tax (PCB)

        var grossIncome = req.BasicSalary + req.Allowances;
        var epfAmount   = Math.Round(req.BasicSalary * epfRate,   2);
        var socsoAmount = Math.Round(req.BasicSalary * socsoRate, 2);
        var taxAmount   = Math.Round(grossIncome     * taxRate,   2);
        var netSalary   = grossIncome - epfAmount - socsoAmount - taxAmount - req.Deductions;

        var payroll = new Payroll
        {
            EmployeeId  = req.EmployeeId,
            Month       = req.Month,
            Year        = req.Year,
            BasicSalary = req.BasicSalary,
            Allowances  = req.Allowances,
            Deductions  = req.Deductions,
            EpfAmount   = epfAmount,
            SocsoAmount = socsoAmount,
            TaxAmount   = taxAmount,
            GrossIncome = grossIncome,
            NetSalary   = netSalary,
            GeneratedOn = DateTime.UtcNow
        };

        _db.Payrolls.Add(payroll);
        await _db.SaveChangesAsync();

        return Ok(payroll);
    }

    /// <summary>DELETE /api/payroll/{id} — delete payroll record</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.Payrolls.FindAsync(id)
            ?? throw new KeyNotFoundException($"Payroll record {id} not found.");

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
}
