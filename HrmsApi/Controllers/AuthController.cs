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

        if (user is null)
            throw new UnauthorizedAccessException("Invalid username or password.");

        // Debug logging
        Console.WriteLine($"Login attempt - Username: {request.Username}, Password provided: {request.Password}");
        Console.WriteLine($"User found - Username: {user.Username}, Hash: {user.PasswordHash}");
        
        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        Console.WriteLine($"Password verification result: {passwordValid}");
        
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        // Find associated employee record (for non-admin users)
        int? employeeId = null;
        if (user.Role.ToLower() != "admin")
        {
            // Match employee by user's email (case-insensitive)
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Email.ToLower() == user.Email.ToLower());
            employeeId = employee?.Id;
            Console.WriteLine($"Employee lookup for email '{user.Email}': {(employee != null ? $"Found Id={employee.Id}" : "Not found")}");
        }

        return Ok(new LoginResponse(token, user.Username, user.Role, expiresAt, employeeId));
    }
    
    /// <summary>POST /api/auth/test-hash — test password hash (temporary debug endpoint)</summary>
    [HttpPost("test-hash")]
    public IActionResult TestHash([FromBody] LoginRequest request)
    {
        var hash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";
        var isValid = BCrypt.Net.BCrypt.Verify(request.Password, hash);
        
        return Ok(new { 
            providedPassword = request.Password,
            expectedHash = hash,
            isValid = isValid,
            message = isValid ? "Password matches!" : "Password does NOT match"
        });
    }
    
    /// <summary>POST /api/auth/generate-hash — generate fresh BCrypt hash for password</summary>
    [HttpPost("generate-hash")]
    public IActionResult GenerateHash([FromBody] LoginRequest request)
    {
        var freshHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var verified = BCrypt.Net.BCrypt.Verify(request.Password, freshHash);
        
        return Ok(new { 
            password = request.Password,
            generatedHash = freshHash,
            verificationTest = verified,
            sqlUpdateCommand = $"UPDATE \"Users\" SET \"PasswordHash\" = '{freshHash}' WHERE \"Username\" = 'admin';"
        });
    }

    /// <summary>
    /// POST /api/auth/test-email — Send a test email to verify SMTP config (Admin only)
    /// </summary>
    [HttpPost("test-email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "NOT SET";
        var smtpPort = _config["Email:SmtpPort"] ?? "NOT SET";
        var smtpUser = _config["Email:SmtpUser"] ?? "NOT SET";
        var fromEmail = _config["Email:FromEmail"] ?? "NOT SET";

        try
        {
            await _emailService.SendEmailAsync(
                request.ToEmail,
                "HRMS Test Email - SMTP Verification",
                $@"<h2>✅ SMTP is working!</h2>
                   <p>This is a test email from your HRMS system.</p>
                   <p><strong>SMTP Host:</strong> {smtpHost}</p>
                   <p><strong>SMTP Port:</strong> {smtpPort}</p>
                   <p><strong>SMTP User:</strong> {smtpUser}</p>
                   <p><strong>From:</strong> {fromEmail}</p>
                   <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>"
            );

            return Ok(new
            {
                success = true,
                message = $"Test email sent to {request.ToEmail}",
                smtpHost, smtpPort, smtpUser, fromEmail
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                error = ex.Message,
                smtpHost, smtpPort, smtpUser, fromEmail,
                hint = smtpUser.Contains("gmail") 
                    ? "Gmail requires an App Password (not your regular password). Enable 2FA then generate App Password at myaccount.google.com/apppasswords"
                    : "Check SMTP credentials and host/port settings"
            });
        }
    }
}

public record TestEmailRequest(string ToEmail);
