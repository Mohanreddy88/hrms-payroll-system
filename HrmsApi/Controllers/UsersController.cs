using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly HrmsDbContext _db;

    public UsersController(HrmsDbContext db) => _db = db;

    /// <summary>
    /// GET /api/users
    /// Returns all users for Admin management (Admin only).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "Admin") return Forbid();

        var users = await _db.Users
            .OrderBy(u => u.Username)
            .Select(u => new
            {
                u.Id, u.Username, u.Email, u.Role,
                u.IsActive, u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// POST /api/users
    /// Creates a new system user (Admin only).
    /// Password is BCrypt-hashed before storage.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserRequest req)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "Admin") return Forbid();

        var exists = await _db.Users.AnyAsync(u => u.Username == req.Username);
        if (exists)
            throw new InvalidOperationException($"Username '{req.Username}' is already taken.");

        // Check for duplicate email
        var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == req.Email.ToLower());
        if (emailExists)
            return BadRequest(new { message = $"Email '{req.Email}' already exists. Please choose another email." });

        // If role is Employee, validate that employee with this email exists
        if (req.Role == "Employee")
        {
            var employeeExists = await _db.Employees.AnyAsync(e => e.Email.ToLower() == req.Email.ToLower());
            if (!employeeExists)
                return BadRequest(new { message = $"No employee found with email '{req.Email}'. Employee role requires matching employee record." });
        }

        var user = new User
        {
            Username     = req.Username,
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role         = req.Role,
            IsActive     = req.IsActive
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.Email, user.Role, user.IsActive, user.CreatedAt });
    }

    /// <summary>
    /// PUT /api/users/{id}
    /// Updates user details. Password is only changed if a new value is provided.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateRequest req)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "Admin") return Forbid();

        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        // Ensure new username is not already taken by another user
        if (user.Username != req.Username)
        {
            var taken = await _db.Users.AnyAsync(u => u.Username == req.Username && u.Id != id);
            if (taken)
                throw new InvalidOperationException($"Username '{req.Username}' is already taken.");
        }

        // Check for duplicate email (excluding current user)
        if (user.Email.ToLower() != req.Email.ToLower())
        {
            var emailTaken = await _db.Users.AnyAsync(u => u.Email.ToLower() == req.Email.ToLower() && u.Id != id);
            if (emailTaken)
                return BadRequest(new { message = $"Email '{req.Email}' already exists. Please choose another email." });
        }

        // If role is Employee, validate that employee with this email exists
        if (req.Role == "Employee")
        {
            var employeeExists = await _db.Employees.AnyAsync(e => e.Email.ToLower() == req.Email.ToLower());
            if (!employeeExists)
                return BadRequest(new { message = $"No employee found with email '{req.Email}'. Employee role requires matching employee record." });
        }

        user.Username = req.Username;
        user.Email    = req.Email;
        user.Role     = req.Role;
        user.IsActive = req.IsActive;

        // Only hash and update password if a new one was provided
        if (!string.IsNullOrWhiteSpace(req.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Username, user.Email, user.Role, user.IsActive, user.CreatedAt });
    }

    /// <summary>
    /// DELETE /api/users/{id}
    /// Permanently removes a user. Blocks deletion of the last Admin.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "Admin") return Forbid();

        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        // Safety: never delete the last remaining admin account
        var adminCount = await _db.Users.CountAsync(u => u.Role == "Admin" && u.IsActive);
        if (user.Role == "Admin" && adminCount == 1)
            throw new InvalidOperationException("Cannot delete the last active Admin user.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
