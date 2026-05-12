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

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return Ok(new LoginResponse(token, user.Username, user.Role, expiresAt));
    }
}
