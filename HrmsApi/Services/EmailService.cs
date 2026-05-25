using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
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

    private const string BrevoApiUrl  = "https://api.brevo.com/v3/smtp/email";
    private const string CompanyName  = "ESYNC SOFTWARE SERVICES SDN.BHD";

    public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
    {
        _config            = config;
        _logger            = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ── Route: use Brevo only when API key is configured, otherwise SMTP ────────
    private async Task SendCoreAsync(string toEmail, string subject, string htmlBody,
        byte[]? attachmentData = null, string? attachmentName = null)
    {
        var brevoKey = _config["Brevo:ApiKey"];
        if (!string.IsNullOrWhiteSpace(brevoKey))
        {
            _logger.LogInformation("Email routing → Brevo API");
            await SendViaBrevoApiAsync(toEmail, subject, htmlBody, brevoKey, attachmentData, attachmentName);
        }
        else
        {
            _logger.LogInformation("Email routing → SMTP (local/Gmail)");
            await SendViaSmtpAsync(toEmail, subject, htmlBody, attachmentData, attachmentName);
        }
    }

    // ── Brevo HTTP API sender (PROD) ─────────────────────────────────────────
    private async Task SendViaBrevoApiAsync(string toEmail, string subject, string htmlBody,
        string apiKey, byte[]? attachmentData = null, string? attachmentName = null)
    {
        var fromEmail = _config["Email:FromEmail"] ?? _config["SMTP_FROM"] ?? _config["Email:SmtpUser"] ?? "noreply@hrms.com";
        var fromName  = _config["Email:FromName"] ?? CompanyName;

        _logger.LogInformation("Brevo API send → to:{To} subject:{Subject}", toEmail, subject);

        var payload = new Dictionary<string, object>
        {
            ["sender"]      = new { name = fromName, email = fromEmail },
            ["to"]          = new[] { new { email = toEmail } },
            ["subject"]     = subject,
            ["htmlContent"] = htmlBody
        };

        if (attachmentData != null && attachmentData.Length > 0 && !string.IsNullOrEmpty(attachmentName))
        {
            payload["attachment"] = new[] { new { name = attachmentName, content = Convert.ToBase64String(attachmentData) } };
        }

        var content  = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var client   = _httpClientFactory.CreateClient("Brevo");
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

        _logger.LogInformation("Email sent via Brevo to {Email}", toEmail);
    }

    // ── SMTP sender (LOCAL / Gmail) ──────────────────────────────────────────
    private async Task SendViaSmtpAsync(string toEmail, string subject, string htmlBody,
        byte[]? attachmentData = null, string? attachmentName = null)
    {
        var host      = _config["Email:SmtpHost"]     ?? "smtp.gmail.com";
        var port      = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var user      = _config["Email:SmtpUser"]     ?? throw new InvalidOperationException("Email:SmtpUser not configured.");
        var password  = _config["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured.");
        var fromEmail = _config["Email:FromEmail"]    ?? user;
        var fromName  = _config["Email:FromName"]     ?? CompanyName;

        _logger.LogInformation("SMTP send → {Host}:{Port} to:{To} subject:{Subject}", host, port, toEmail, subject);

        using var message = new MailMessage();
        message.From       = new MailAddress(fromEmail, fromName);
        message.To.Add(toEmail);
        message.Subject    = subject;
        message.Body       = htmlBody;
        message.IsBodyHtml = true;

        if (attachmentData != null && attachmentData.Length > 0 && !string.IsNullOrEmpty(attachmentName))
        {
            var stream     = new MemoryStream(attachmentData);
            var attachment = new Attachment(stream, attachmentName, "application/pdf");
            message.Attachments.Add(attachment);
        }

        using var smtp = new SmtpClient(host, port)
        {
            Credentials  = new NetworkCredential(user, password),
            EnableSsl    = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await smtp.SendMailAsync(message);
        _logger.LogInformation("Email sent via SMTP to {Email}", toEmail);
    }

    // ── Public interface methods ──────────────────────────────────────────────

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try   { await SendCoreAsync(toEmail, subject, body); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Message}", toEmail, ex.Message);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
    }

    public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body,
        byte[] attachmentData, string attachmentName)
    {
        try   { await SendCoreAsync(toEmail, subject, body, attachmentData, attachmentName); }
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
  body{{font-family:Arial,sans-serif;line-height:1.8;color:#1a1a1a;background:#f4f4f4;margin:0;padding:0;}}
  .wrap{{max-width:580px;margin:30px auto;background:white;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);}}
  .header{{background:#1e40af;padding:24px 30px;}}
  .header h2{{margin:0;color:white;font-size:18px;font-weight:600;}}
  .header p{{margin:4px 0 0;color:#93c5fd;font-size:13px;}}
  .body{{padding:28px 30px;}}
  .body p{{margin:0 0 12px;font-size:14px;color:#374151;}}
  .info-table{{width:100%;border-collapse:collapse;margin:16px 0;font-size:13px;}}
  .info-table td{{padding:8px 12px;border-bottom:1px solid #f1f5f9;}}
  .info-table td:first-child{{color:#6b7280;width:40%;}}
  .info-table td:last-child{{font-weight:600;color:#111827;}}
  .notice{{background:#f0f9ff;border-left:4px solid #0ea5e9;padding:12px 16px;border-radius:4px;font-size:13px;color:#0369a1;margin-top:16px;}}
  .footer{{background:#f9fafb;padding:16px 30px;text-align:center;font-size:11px;color:#9ca3af;border-top:1px solid #f1f5f9;}}
</style></head><body>
  <div class=""wrap"">
    <div class=""header"">
      <h2>ESYNC SOFTWARE SERVICES SDN.BHD</h2>
      <p>Payslip Notification — {month} {year}</p>
    </div>
    <div class=""body"">
      <p>Dear <strong>{employeeName}</strong>,</p>
      <p>Please find your payslip for <strong>{month} {year}</strong> attached to this email.</p>
      <div class=""notice"">
        Your detailed payslip PDF is attached. Please login to the HRMS portal for full payroll details.
      </div>
    </div>
    <div class=""footer"">
      This is an auto-generated email. Please do not reply. &copy; {DateTime.Now.Year} {CompanyName}
    </div>
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
