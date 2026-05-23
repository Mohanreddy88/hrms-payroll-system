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
    public string Email        { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Month        { get; set; } = string.Empty;
    public string Year         { get; set; } = string.Empty;
    public decimal NetSalary   { get; set; }
    public int    PayslipId    { get; set; }
}

public class EmailService : IEmailService
{
    private readonly IConfiguration        _config;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory    _httpClientFactory;

    // Brevo transactional email HTTP API — works on Railway (no SMTP port blocking)
    private const string BrevoApiUrl = "https://api.brevo.com/v3/smtp/email";

    public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
    {
        _config            = config;
        _logger            = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ── Core sender — Brevo HTTP API ─────────────────────────────────────────
    private async Task SendViaBrevoApiAsync(string toEmail, string subject, string htmlBody,
        byte[]? attachmentData = null, string? attachmentName = null)
    {
        // Prefer BREVO_API_KEY (xkeysib-...) → fallback to SMTP_PASSWORD (xsmtpsib-...)
        // Brevo HTTP API requires the API key from Settings → API Keys, NOT the SMTP key
        var apiKey   = _config["Brevo:ApiKey"]
                    ?? _config["Email:SmtpPassword"]
                    ?? throw new InvalidOperationException("Brevo API key not configured. Set BREVO_API_KEY in Railway env vars.");

        var fromEmail = _config["Email:FromEmail"]
                     ?? _config["SMTP_FROM"]
                     ?? _config["Email:SmtpUser"]
                     ?? "noreply@hrms.com";

        var fromName  = _config["Email:FromName"] ?? "HRMS Payroll System";

        _logger.LogInformation("Brevo API send → to:{To} subject:{Subject} from:{From}", toEmail, subject, fromEmail);

        var payload = new Dictionary<string, object>
        {
            ["sender"]   = new { name = fromName, email = fromEmail },
            ["to"]       = new[] { new { email = toEmail } },
            ["subject"]  = subject,
            ["htmlContent"] = htmlBody
        };

        // Add PDF attachment if provided
        if (attachmentData != null && attachmentData.Length > 0 && !string.IsNullOrEmpty(attachmentName))
        {
            payload["attachment"] = new[]
            {
                new
                {
                    name    = attachmentName,
                    content = Convert.ToBase64String(attachmentData)
                }
            };
        }

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient("Brevo");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response     = await client.PostAsync(BrevoApiUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Brevo API error {Status}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Brevo API failed ({response.StatusCode}): {responseBody}");
        }

        _logger.LogInformation("Email sent via Brevo API to {Email} | subject: {Subject}", toEmail, subject);
    }

    // ── Public interface methods ──────────────────────────────────────────────

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try   { await SendViaBrevoApiAsync(toEmail, subject, body); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Message}", toEmail, ex.Message);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
    }

    public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body,
        byte[] attachmentData, string attachmentName)
    {
        try   { await SendViaBrevoApiAsync(toEmail, subject, body, attachmentData, attachmentName); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {Email}", toEmail);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
    }

    public async Task SendPayslipEmailAsync(string toEmail, string employeeName, string month, string year, decimal netSalary)
    {
        var subject = $"Payslip for {month}/{year}";
        var body    = GeneratePayslipEmailBody(employeeName, month, year, netSalary);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendBulkPayslipEmailsAsync(List<PayslipEmailData> recipients)
    {
        // Send sequentially to avoid rate-limiting on free tier
        foreach (var r in recipients)
        {
            try   { await SendPayslipEmailAsync(r.Email, r.EmployeeName, r.Month, r.Year, r.NetSalary); }
            catch (Exception ex) { _logger.LogError(ex, "Failed bulk payslip email to {Email}", r.Email); }
        }
        _logger.LogInformation("Bulk payslip emails completed for {Count} recipients", recipients.Count);
    }

    public async Task SendAttendanceApprovedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate)
    {
        var subject = $"Attendance Approved — {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";
        var body    = GenerateAttendanceApprovedEmailBody(employeeName, startDate, endDate);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendAttendanceRejectedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate, string rejectionReason)
    {
        var subject = $"Attendance Rejected — {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";
        var body    = GenerateAttendanceRejectedEmailBody(employeeName, startDate, endDate, rejectionReason);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendLeaveApprovedEmailAsync(string toEmail, string employeeName, string leaveTypeName,
        DateTime startDate, DateTime endDate, decimal totalDays, string remarks)
    {
        var subject = $"Leave Approved — {leaveTypeName} ({startDate:dd MMM} - {endDate:dd MMM yyyy})";
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
        var subject = $"Leave Rejected — {leaveTypeName} ({startDate:dd MMM} - {endDate:dd MMM yyyy})";
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
      <p>Your leave request has been <strong>rejected</strong>.</p>
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

    // ── HTML body generators ──────────────────────────────────────────────────

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
}
