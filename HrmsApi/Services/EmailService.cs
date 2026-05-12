using System.Net;
using System.Net.Mail;

namespace HrmsApi.Services;

public interface IEmailService
{
    Task SendPayslipEmailAsync(string toEmail, string employeeName, string month, string year, decimal netSalary);
    Task SendBulkPayslipEmailsAsync(List<PayslipEmailData> recipients);
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentData, string attachmentName);
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
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"] ?? throw new InvalidOperationException("Email:SmtpUser not configured");
            var smtpPass = _config["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
            var fromEmail = _config["Email:FromEmail"] ?? smtpUser;
            var fromName = _config["Email:FromName"] ?? "HRMS Payroll";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

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
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"] ?? throw new InvalidOperationException("Email:SmtpUser not configured");
            var smtpPass = _config["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
            var fromEmail = _config["Email:FromEmail"] ?? smtpUser;
            var fromName = _config["Email:FromName"] ?? "HRMS Payroll";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = $"Payslip for {month}/{year}",
                Body = GeneratePayslipEmailBody(employeeName, month, year, netSalary),
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Payslip email sent to {Email} for {Month}/{Year}", toEmail, month, year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payslip email to {Email}", toEmail);
            throw;
        }
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
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"] ?? throw new InvalidOperationException("Email:SmtpUser not configured");
            var smtpPass = _config["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
            var fromEmail = _config["Email:FromEmail"] ?? smtpUser;
            var fromName = _config["Email:FromName"] ?? "HRMS Payroll";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            // Add attachment
            if (attachmentData != null && attachmentData.Length > 0)
            {
                var stream = new MemoryStream(attachmentData);
                var attachment = new Attachment(stream, attachmentName);
                mailMessage.Attachments.Add(attachment);
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
