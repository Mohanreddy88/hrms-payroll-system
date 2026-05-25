using HrmsApi.Data;
using HrmsApi.Models;
using HrmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public AuthController(HrmsDbContext db, TokenService tokenService, IEmailService emailService, IConfiguration config)
    {
        _db           = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _config       = config;
    }

    /// <summary>POST /api/auth/login — authenticate and return JWT</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        // Find associated employee record (for non-admin users)
        int? employeeId = null;
        if (user.Role.ToLower() != "admin")
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());
            employeeId = employee?.Id;
        }

        return Ok(new LoginResponse(token, user.Username, user.Role, expiresAt, employeeId, user.MustChangePassword));
    }

    /// <summary>POST /api/auth/forgot-password — send reset token to user's email</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Always return success to avoid email enumeration
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (user is null || !user.IsActive)
            return Ok(new { message = "If that email exists, a reset link has been sent." });

        // Generate a secure random token
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken       = BCrypt.Net.BCrypt.HashPassword(token);
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:4200";
        var resetLink   = $"{frontendUrl}/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";

        var body = $@"<!DOCTYPE html><html><head><style>
  body{{font-family:Arial,sans-serif;line-height:1.6;color:#333;}}
  .container{{max-width:600px;margin:0 auto;padding:20px;}}
  .header{{background:#2563eb;color:white;padding:20px;text-align:center;border-radius:8px 8px 0 0;}}
  .content{{background:#f8f9fa;padding:30px;border-radius:0 0 8px 8px;}}
  .btn{{display:inline-block;background:#2563eb;color:white;padding:12px 32px;border-radius:8px;text-decoration:none;font-weight:600;margin:20px 0;}}
  .footer{{text-align:center;margin-top:20px;color:#888;font-size:12px;}}
  .note{{font-size:12px;color:#888;margin-top:16px;}}
</style></head><body>
  <div class=""container"">
    <div class=""header""><h1>&#x1F510; Password Reset Request</h1></div>
    <div class=""content"">
      <p>Dear <strong>{user.Username}</strong>,</p>
      <p>We received a request to reset your HRMS password. Click the button below to set a new password:</p>
      <p style=""text-align:center;""><a href=""{resetLink}"" class=""btn"">Reset Password</a></p>
      <p class=""note"">This link expires in <strong>1 hour</strong>. If you did not request a password reset, please ignore this email — your account remains secure.</p>
      <p class=""note"">If the button doesn't work, copy and paste this link:<br/><a href=""{resetLink}"">{resetLink}</a></p>
    </div>
    <div class=""footer""><p>&copy; {DateTime.Now.Year} HRMS. All rights reserved.</p></div>
  </div>
</body></html>";

        await _emailService.SendEmailAsync(user.Email, "HRMS – Password Reset Request", body);

        return Ok(new { message = "If that email exists, a reset link has been sent." });
    }

    /// <summary>POST /api/auth/reset-password — validate token and set new password</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Token and new password are required." });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        // Find users with a non-expired reset token
        var candidates = await _db.Users
            .Where(u => u.PasswordResetToken != null && u.PasswordResetTokenExpiry > DateTime.UtcNow)
            .ToListAsync();

        var user = candidates.FirstOrDefault(u =>
            BCrypt.Net.BCrypt.Verify(request.Token, u.PasswordResetToken!));

        if (user is null)
            return BadRequest(new { message = "Invalid or expired reset token." });

        user.PasswordHash             = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken       = null;
        user.PasswordResetTokenExpiry = null;
        user.MustChangePassword       = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password has been reset successfully. You may now log in." });
    }

    /// <summary>POST /api/auth/test-email — Send a test email (Admin only)</summary>
    [HttpPost("test-email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
    {
        var smtpHost  = _config["Email:SmtpHost"]  ?? "NOT SET";
        var smtpPort  = _config["Email:SmtpPort"]  ?? "NOT SET";
        var smtpUser  = _config["Email:SmtpUser"]  ?? "NOT SET";
        var fromEmail = _config["Email:FromEmail"] ?? "NOT SET";

        try
        {
            await _emailService.SendEmailAsync(
                request.ToEmail,
                "HRMS Test Email - SMTP Verification",
                $@"<h2>✅ SMTP is working!</h2>
                   <p>This is a test email from your HRMS system.</p>
                   <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>"
            );

            return Ok(new { success = true, message = $"Test email sent to {request.ToEmail}" });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                error   = ex.Message,
                hint    = smtpUser.Contains("gmail")
                    ? "Gmail requires an App Password. Enable 2FA then generate one at myaccount.google.com/apppasswords"
                    : "Check SMTP credentials and host/port settings"
            });
        }
    }

    /// <summary>Temporary debug — shows masked config values to verify env vars loaded on Railway</summary>
    [HttpGet("debug-config")]
    [Authorize(Roles = "Admin")]
    public IActionResult DebugConfig()
    {
        string Mask(string? val) => string.IsNullOrEmpty(val)
            ? "NOT SET"
            : val.Length <= 8 ? "***" : $"{val[..4]}...{val[^4..]} (len:{val.Length})";

        return Ok(new
        {
            smtpHost        = _config["Email:SmtpHost"],
            smtpPort        = _config["Email:SmtpPort"],
            smtpUser        = _config["Email:SmtpUser"],
            smtpPassword    = Mask(_config["Email:SmtpPassword"]),
            fromEmail       = _config["Email:FromEmail"],
            fromName        = _config["Email:FromName"],
            brevoApiKey     = Mask(_config["Brevo:ApiKey"]),
            envBrevoApiKey  = Mask(Environment.GetEnvironmentVariable("BREVO_API_KEY")),
            envSmtpPassword = Mask(Environment.GetEnvironmentVariable("SMTP_PASSWORD")),
            willUseKey      = Mask(_config["Brevo:ApiKey"] ?? _config["Email:SmtpPassword"])
        });
    }
}

public record TestEmailRequest(string ToEmail);
