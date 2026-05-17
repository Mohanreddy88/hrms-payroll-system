using System.Net;
using System.Net.Mail;

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

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Sends a generic email with custom subject and HTML body
    /// </summary>
    // ── Shared SMTP builder ──────────────────────────────────────────────────
    private (SmtpClient client, string fromEmail, string fromName) BuildSmtpClient()
    {
        var smtpHost  = _config["Email:SmtpHost"]     ?? "smtp.gmail.com";
        var smtpPort  = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var smtpUser  = _config["Email:SmtpUser"]     ?? throw new InvalidOperationException("Email:SmtpUser not configured");
        var smtpPass  = _config["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
        var fromEmail = _config["Email:FromEmail"]    ?? smtpUser;
        var fromName  = _config["Email:FromName"]     ?? "HRMS Payroll";

        var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl   = true,
            Timeout     = 30_000   // 30 seconds — prevents premature timeout on attachments
        };

        _logger.LogDebug("SMTP: {Host}:{Port} user={User}", smtpHost, smtpPort, smtpUser);
        return (client, fromEmail, fromName);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var (client, fromEmail, fromName) = BuildSmtpClient();
            using var _ = client;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Sends a payslip notification email to an employee
    /// </summary>
    public async Task SendPayslipEmailAsync(string toEmail, string employeeName, string month, string year, decimal netSalary)
    {
        try
        {
            var (client, fromEmail, fromName) = BuildSmtpClient();
            using var _ = client;
            var msg = new MailMessage { From = new MailAddress(fromEmail, fromName), Subject = $"Payslip for {month}/{year}", Body = GeneratePayslipEmailBody(employeeName, month, year, netSalary), IsBodyHtml = true };
            msg.To.Add(toEmail);
            await client.SendMailAsync(msg);
            _logger.LogInformation("Payslip email sent to {Email} for {Month}/{Year}", toEmail, month, year);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send payslip email to {Email}", toEmail); throw; }
    }

    /// <summary>
    /// Sends payslip emails to multiple employees in bulk
    /// </summary>
    public async Task SendBulkPayslipEmailsAsync(List<PayslipEmailData> recipients)
    {
        var tasks = recipients.Select(r =>
            SendPayslipEmailAsync(r.Email, r.EmployeeName, r.Month, r.Year, r.NetSalary)
        );

        await Task.WhenAll(tasks);
        _logger.LogInformation("Sent {Count} payslip emails", recipients.Count);
    }

    /// <summary>
    /// Sends an email with file attachment
    /// </summary>
    public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentData, string attachmentName)
    {
        try
        {
            var (client, fromEmail, fromName) = BuildSmtpClient();
            using var _ = client;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            if (attachmentData != null && attachmentData.Length > 0)
            {
                var stream = new MemoryStream(attachmentData);
                mailMessage.Attachments.Add(new Attachment(stream, attachmentName));
            }

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email with attachment sent to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {Email}", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Sends attendance approved notification email
    /// </summary>
    public async Task SendAttendanceApprovedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate)
    {
        var subject = $"Attendance Period Approved - {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";
        var body = GenerateAttendanceApprovedEmailBody(employeeName, startDate, endDate);
        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends attendance rejected notification email
    /// </summary>
    public async Task SendAttendanceRejectedEmailAsync(string toEmail, string employeeName, DateTime startDate, DateTime endDate, string rejectionReason)
    {
        var subject = $"Attendance Period Rejected - {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";
        var body = GenerateAttendanceRejectedEmailBody(employeeName, startDate, endDate, rejectionReason);
        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>Sends leave approved notification email</summary>
    public async Task SendLeaveApprovedEmailAsync(string toEmail, string employeeName, string leaveTypeName, DateTime startDate, DateTime endDate, decimal totalDays, string remarks)
    {
        var subject = $"Leave Request Approved - {leaveTypeName} ({startDate:dd MMM} - {endDate:dd MMM yyyy})";
        var body = $@"
<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;line-height:1.6;color:#333;}}
  .container{{max-width:600px;margin:0 auto;padding:20px;}}
  .header{{background:#10b981;color:white;padding:20px;text-align:center;border-radius:8px 8px 0 0;}}
  .content{{background:#f8f9fa;padding:30px;border-radius:0 0 8px 8px;}}
  .info-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;border-left:4px solid #10b981;}}
  .footer{{text-align:center;margin-top:20px;color:#888;font-size:12px;}}
</style></head><body>
  <div class=""container"">
    <div class=""header""><h1>✅ Leave Request Approved</h1></div>
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
      <p style=""margin-top:20px;font-size:13px;color:#666;"">This is an automated email. Please do not reply.</p>
    </div>
    <div class=""footer""><p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p></div>
  </div>
</body></html>";
        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>Sends leave rejected notification email</summary>
    public async Task SendLeaveRejectedEmailAsync(string toEmail, string employeeName, string leaveTypeName, DateTime startDate, DateTime endDate, string remarks)
    {
        var subject = $"Leave Request Rejected - {leaveTypeName} ({startDate:dd MMM} - {endDate:dd MMM yyyy})";
        var body = $@"
<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;line-height:1.6;color:#333;}}
  .container{{max-width:600px;margin:0 auto;padding:20px;}}
  .header{{background:#ef4444;color:white;padding:20px;text-align:center;border-radius:8px 8px 0 0;}}
  .content{{background:#f8f9fa;padding:30px;border-radius:0 0 8px 8px;}}
  .info-box{{background:white;padding:20px;border-radius:8px;margin:20px 0;border-left:4px solid #ef4444;}}
  .footer{{text-align:center;margin-top:20px;color:#888;font-size:12px;}}
</style></head><body>
  <div class=""container"">
    <div class=""header""><h1>❌ Leave Request Rejected</h1></div>
    <div class=""content"">
      <p>Dear <strong>{employeeName}</strong>,</p>
      <p>Unfortunately, your leave request has been <strong>rejected</strong>.</p>
      <div class=""info-box"">
        <p><strong>Leave Type:</strong> {leaveTypeName}</p>
        <p><strong>Period:</strong> {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}</p>
        {(string.IsNullOrWhiteSpace(remarks) ? "" : $"<p><strong>Reason:</strong> {remarks}</p>")}
      </div>
      <p>Please contact HR if you have any questions.</p>
      <p style=""margin-top:20px;font-size:13px;color:#666;"">This is an automated email. Please do not reply.</p>
    </div>
    <div class=""footer""><p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p></div>
  </div>
</body></html>";
        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Generates HTML email body for attendance approved notification
    /// </summary>
    private string GenerateAttendanceApprovedEmailBody(string employeeName, DateTime startDate, DateTime endDate)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #10b981; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #10b981; }}
        .footer {{ text-align: center; margin-top: 20px; color: #888; font-size: 12px; }}
        .success-icon {{ font-size: 48px; text-align: center; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✅ Attendance Approved</h1>
        </div>
        <div class=""content"">
            <div class=""success-icon"">✓</div>
            <p>Dear <strong>{employeeName}</strong>,</p>
            <p>Your attendance period has been <strong>approved</strong> by the administrator.</p>
            
            <div class=""info-box"">
                <p style=""margin: 0; font-size: 14px; color: #666;""><strong>Period:</strong></p>
                <p style=""margin: 5px 0; font-size: 16px; color: #000;"">{startDate:dddd, dd MMMM yyyy} - {endDate:dddd, dd MMMM yyyy}</p>
            </div>

            <p>No further action is required from you.</p>
            
            <p style=""margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 13px; color: #666;"">
                <strong>Note:</strong> This is an automated email. Please do not reply to this message.
            </p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generates HTML email body for attendance rejected notification
    /// </summary>
    private string GenerateAttendanceRejectedEmailBody(string employeeName, DateTime startDate, DateTime endDate, string rejectionReason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #ef4444; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ef4444; }}
        .footer {{ text-align: center; margin-top: 20px; color: #888; font-size: 12px; }}
        .warning-icon {{ font-size: 48px; text-align: center; margin: 20px 0; color: #ef4444; }}
        .btn {{ display: inline-block; padding: 12px 24px; background: #2563eb; color: white; text-decoration: none; border-radius: 6px; margin-top: 15px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>⚠️ Attendance Rejected</h1>
        </div>
        <div class=""content"">
            <div class=""warning-icon"">✗</div>
            <p>Dear <strong>{employeeName}</strong>,</p>
            <p>Your attendance period has been <strong>rejected</strong> by the administrator.</p>
            
            <div class=""info-box"">
                <p style=""margin: 0; font-size: 14px; color: #666;""><strong>Period:</strong></p>
                <p style=""margin: 5px 0 15px 0; font-size: 16px; color: #000;"">{startDate:dddd, dd MMMM yyyy} - {endDate:dddd, dd MMMM yyyy}</p>
                <p style=""margin: 0; font-size: 14px; color: #666;""><strong>Reason:</strong></p>
                <p style=""margin: 5px 0; font-size: 15px; color: #dc2626; font-weight: 500;"">{rejectionReason}</p>
            </div>

            <p>Please log in to the HRMS portal, amend your attendance data according to the reason above, and resubmit for approval.</p>
            
            <p style=""margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 13px; color: #666;"">
                <strong>Note:</strong> This is an automated email. Please do not reply to this message.
            </p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generates HTML email body for payslip notification
    /// </summary>
    private string GeneratePayslipEmailBody(string employeeName, string month, string year, decimal netSalary)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #2563eb; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .salary-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; text-align: center; border-left: 4px solid #10b981; }}
        .salary-amount {{ font-size: 32px; font-weight: bold; color: #10b981; }}
        .footer {{ text-align: center; margin-top: 20px; color: #888; font-size: 12px; }}
        .btn {{ display: inline-block; padding: 12px 24px; background: #2563eb; color: white; text-decoration: none; border-radius: 6px; margin-top: 15px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🧾 Payslip Available</h1>
        </div>
        <div class=""content"">
            <p>Dear <strong>{employeeName}</strong>,</p>
            <p>Your payslip for <strong>{month}/{year}</strong> is now available.</p>
            
            <div class=""salary-box"">
                <p style=""margin: 0; font-size: 14px; color: #666;"">Net Salary</p>
                <div class=""salary-amount"">RM {netSalary:N2}</div>
            </div>

            <p>Please log in to the HRMS portal to view and download your detailed payslip.</p>
            
            <p style=""margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 13px; color: #666;"">
                <strong>Note:</strong> This is an automated email. Please do not reply to this message. 
                For any payroll inquiries, please contact HR department.
            </p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
