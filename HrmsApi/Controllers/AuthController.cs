using HrmsApi.Data;
using HrmsApi.Models;
using HrmsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(HrmsDbContext db, TokenService tokenService)
    {
        _db           = db;
        _tokenService = tokenService;
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
}
