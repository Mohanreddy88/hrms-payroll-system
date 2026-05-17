using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HrmsApi.Services;

public interface IEmailService
{
    Task SendPayslipEmailAsync(string toEmail, string employeeName, string month, string year, decimal netSalary);
    Task SendBulkPayslipEmailsAsync(List<PayslipEmailData> recipients);
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentData, string attachmentName);
    Task SendAttendanceApprovedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate);
    Task SendAttendanceRejectedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate, string rejectionReason);
    Task SendLeaveApprovedEmailAsync(string toEmail, string employeeName, string leaveTypeName, DateTime startDate, DateTime endDate, decimal totalDays, string remarks);
    Task SendLeaveRejectedEmailAsync(string toEmail, string employeeName, string leaveTypeName, DateTime startDate, DateTime endDate, string remarks);
}

public class PayslipEmailData
{
    public string Email { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public decimal NetSalary { get; set; }
    public int PayslipId { get; set; }
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string ResendApiUrl = "https://api.resend.com/emails";

    public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ── Core Resend HTTP sender ───────────────────────────────────────────────
    private async Task SendViaResendAsync(string toEmail, string subject, string htmlBody,
        byte[]? attachmentData = null, string? attachmentName = null)
    {
        var apiKey   = _config["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey not configured");
        var fromName = _config["Email:FromName"]  ?? "HRMS Payroll";
        var fromEmail= _config["Resend:FromEmail"] ?? "onboarding@resend.dev";

        var payload = new Dictionary<string, object>
        {
            ["from"]    = $"{fromName} <{fromEmail}>",
            ["to"]      = new[] { toEmail },
            ["subject"] = subject,
            ["html"]    = htmlBody
        };

        // Add attachment if provided (Resend supports base64 attachments)
        if (attachmentData != null && attachmentData.Length > 0 && !string.IsNullOrEmpty(attachmentName))
        {
            payload["attachments"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["filename"] = attachmentName,
                    ["content"]  = Convert.ToBase64String(attachmentData)
                }
            };
        }

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient("Resend");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        _logger.LogDebug("Sending email via Resend to {Email}, subject: {Subject}", toEmail, subject);

        var response = await client.PostAsync(ResendApiUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Resend API error {Status}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Resend API failed ({response.StatusCode}): {responseBody}");
        }

        _logger.LogInformation("Email sent via Resend to {Email} | subject: {Subject}", toEmail, subject);
    }

    // ── Public interface methods ──────────────────────────────────────────────

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            await SendViaResendAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body,
        byte[] attachmentData, string attachmentName)
    {
        try
        {
            await SendViaResendAsync(toEmail, subject, body, attachmentData, attachmentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendPayslipEmailAsync(string toEmail, string employeeName, string month, string year, decimal netSalary)
    {
        try
        {
            var subject = $"Payslip for {month}/{year}";
            var body    = GeneratePayslipEmailBody(employeeName, month, year, netSalary);
            await SendViaResendAsync(toEmail, subject, body);
            _logger.LogInformation("Payslip email sent to {Email} for {Month}/{Year}", toEmail, month, year);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send payslip email to {Email}", toEmail); throw; }
    }

    public async Task SendBulkPayslipEmailsAsync(List<PayslipEmailData> recipients)
    {
        var tasks = recipients.Select(r =>
            SendPayslipEmailAsync(r.Email, r.EmployeeName, r.Month, r.Year, r.NetSalary));
        await Task.WhenAll(tasks);
        _logger.LogInformation("Sent {Count} payslip emails via Resend", recipients.Count);
    }

    public async Task SendAttendanceApprovedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate)
    {
        var subject = $"Attendance Period Approved - {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";
        var body    = GenerateAttendanceApprovedEmailBody(employeeName, startDate, endDate);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendAttendanceRejectedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate, string rejectionReason)
    {
        var subject = $"Attendance Period Rejected - {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";
        var body    = GenerateAttendanceRejectedEmailBody(employeeName, startDate, endDate, rejectionReason);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendLeaveApprovedEmailAsync(string toEmail, string employeeName, string leaveTypeName,
        DateTime startDate, DateTime endDate, decimal totalDays, string remarks)
    {
        var subject = $"Leave Request Approved - {leaveTypeName} ({startDate:dd MMM} - {endDate:dd MMM yyyy})";
        var body = $@"<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;line-height:1.6;color:#333;}}
  .container{{max-width:600px;margin:0 auto;padding:20px;}}
  .header{{background:#10b981;color:white;padding:20px;text-align:center;border-radius:8px 8px 0 0;}}
  .content{{background:#f8f9fa;padding:30px;border-radius:0 0 8px 8px;}}
  .info-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;border-left:4px solid #10b981;}}
  .footer{{text-align:center;margin-top:20px;color:#888;font-size:12px;}}
</style></head><body>
  <div class=""container"">
    <div class=""header""><h1>&#x2705; Leave Request Approved</h1></div>
    <div class=""content"">
      <p>Dear <strong>{employeeName}</strong>,</p>
      <p>Your leave request has been <strong>approved</strong>.</p>
      <div class=""info-box"">
        <p><strong>Leave Type:</strong> {leaveTypeName}</p>
        <p><strong>Period:</strong> {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}</p>
        <p><strong>Total Days:</strong> {totalDays}</p>
        {(string.IsNullOrWhiteSpace(remarks) ? "" : $"<p><strong>Remarks:</strong> {remarks}</p>")}
      </div>
      <p>Your leave balance has been updated accordingly.</p>
    </div>
    <div class=""footer""><p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p></div>
  </div>
</body></html>";
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendLeaveRejectedEmailAsync(string toEmail, string employeeName, string leaveTypeName,
        DateTime startDate, DateTime endDate, string remarks)
    {
        var subject = $"Leave Request Rejected - {leaveTypeName} ({startDate:dd MMM} - {endDate:dd MMM yyyy})";
        var body = $@"<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;line-height:1.6;color:#333;}}
  .container{{max-width:600px;margin:0 auto;padding:20px;}}
  .header{{background:#ef4444;color:white;padding:20px;text-align:center;border-radius:8px 8px 0 0;}}
  .content{{background:#f8f9fa;padding:30px;border-radius:0 0 8px 8px;}}
  .info-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;border-left:4px solid #ef4444;}}
  .footer{{text-align:center;margin-top:20px;color:#888;font-size:12px;}}
</style></head><body>
  <div class=""container"">
    <div class=""header""><h1>&#x274C; Leave Request Rejected</h1></div>
    <div class=""content"">
      <p>Dear <strong>{employeeName}</strong>,</p>
      <p>Unfortunately, your leave request has been <strong>rejected</strong>.</p>
      <div class=""info-box"">
        <p><strong>Leave Type:</strong> {leaveTypeName}</p>
        <p><strong>Period:</strong> {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}</p>
        {(string.IsNullOrWhiteSpace(remarks) ? "" : $"<p><strong>Reason:</strong> {remarks}</p>")}
      </div>
      <p>Please contact HR if you have any questions.</p>
    </div>
    <div class=""footer""><p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p></div>
  </div>
</body></html>";
        await SendEmailAsync(toEmail, subject, body);
    }

    // ── Private HTML body generators ─────────────────────────────────────────

    private string GenerateAttendanceApprovedEmailBody(string employeeName, DateTime startDate, DateTime endDate) =>
        $@"<!DOCTYPE html><html><head><style>
        body{{font-family:Arial,sans-serif;line-height:1.6;color:#333;}}
        .container{{max-width:600px;margin:0 auto;padding:20px;}}
        .header{{background:#10b981;color:white;padding:20px;text-align:center;border-radius:8px 8px 0 0;}}
        .content{{background:#f8f9fa;padding:30px;border-radius:0 0 8px 8px;}}
        .info-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;border-left:4px solid #10b981;}}
        .footer{{text-align:center;margin-top:20px;color:#888;font-size:12px;}}
    </style></head><body>
        <div class=""container"">
            <div class=""header""><h1>&#x2705; Attendance Approved</h1></div>
            <div class=""content"">
                <p>Dear <strong>{employeeName}</strong>,</p>
                <p>Your attendance period has been <strong>approved</strong>.</p>
                <div class=""info-box"">
                    <p><strong>Period:</strong> {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}</p>
                </div>
                <p>No further action is required.</p>
            </div>
            <div class=""footer""><p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p></div>
        </div>
    </body></html>";

    private string GenerateAttendanceRejectedEmailBody(string employeeName, DateTime startDate, DateTime endDate, string rejectionReason) =>
        $@"<!DOCTYPE html><html><head><style>
        body{{font-family:Arial,sans-serif;line-height:1.6;color:#333;}}
        .container{{max-width:600px;margin:0 auto;padding:20px;}}
        .header{{background:#ef4444;color:white;padding:20px;text-align:center;border-radius:8px 8px 0 0;}}
        .content{{background:#f8f9fa;padding:30px;border-radius:0 0 8px 8px;}}
        .info-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;border-left:4px solid #ef4444;}}
        .footer{{text-align:center;margin-top:20px;color:#888;font-size:12px;}}
    </style></head><body>
        <div class=""container"">
            <div class=""header""><h1>&#x26A0;&#xFE0F; Attendance Rejected</h1></div>
            <div class=""content"">
                <p>Dear <strong>{employeeName}</strong>,</p>
                <p>Your attendance period has been <strong>rejected</strong>.</p>
                <div class=""info-box"">
                    <p><strong>Period:</strong> {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}</p>
                    <p><strong>Reason:</strong> {rejectionReason}</p>
                </div>
                <p>Please amend and resubmit your attendance.</p>
            </div>
            <div class=""footer""><p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p></div>
        </div>
    </body></html>";

    private string GeneratePayslipEmailBody(string employeeName, string month, string year, decimal netSalary) =>
        $@"<!DOCTYPE html><html><head><style>
        body{{font-family:Arial,sans-serif;line-height:1.6;color:#333;}}
        .container{{max-width:600px;margin:0 auto;padding:20px;}}
        .header{{background:#2563eb;color:white;padding:20px;text-align:center;border-radius:8px 8px 0 0;}}
        .content{{background:#f8f9fa;padding:30px;border-radius:0 0 8px 8px;}}
        .salary-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;text-align:center;border-left:4px solid #10b981;}}
        .salary-amount{{font-size:32px;font-weight:bold;color:#10b981;}}
        .footer{{text-align:center;margin-top:20px;color:#888;font-size:12px;}}
    </style></head><body>
        <div class=""container"">
            <div class=""header""><h1>&#x1F9FE; Payslip Available</h1></div>
            <div class=""content"">
                <p>Dear <strong>{employeeName}</strong>,</p>
                <p>Your payslip for <strong>{month}/{year}</strong> is now available.</p>
                <div class=""salary-box"">
                    <p style=""margin:0;font-size:14px;color:#666;"">Net Salary</p>
                    <div class=""salary-amount"">RM {netSalary:N2}</div>
                </div>
                <p>Please log in to the HRMS portal to view your detailed payslip.</p>
            </div>
            <div class=""footer""><p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p></div>
        </div>
    </body></html>";
}
