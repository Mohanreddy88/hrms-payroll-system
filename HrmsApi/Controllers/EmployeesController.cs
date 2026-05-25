using HrmsApi.Data;
using HrmsApi.Models;
using HrmsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using BCrypt.Net;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(HrmsDbContext db, IAuditService audit, ILogger<EmployeesController> logger)
    {
        _db     = db;
        _audit  = audit;
        _logger = logger;
    }

    private string CurrentUser() => User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
    private int?   CurrentUserId() { var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; return int.TryParse(v, out var id) ? id : null; }

    /// <summary>
    /// GET /api/employees
    /// Returns all employees with their department name and bank name for display in list/dropdown.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Bank)
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id, e.EmployeeCode, e.Name, e.Email, e.Phone,
                e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.Designation,
                e.JoinDate, e.Salary, e.IsActive,
                e.BankId, e.AccountNumber,
                BankName = e.Bank != null ? e.Bank.Name : null,
                e.Gender
            })
            .ToListAsync();

        // Remove duplicates in memory
        var uniqueEmployees = employees
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();

        return Ok(uniqueEmployees);
    }

    /// <summary>
    /// GET /api/employees/active
    /// Returns only active employees (IsActive = true) for dropdowns and filters.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var employees = await _db.Employees
            .Where(e => e.IsActive)
            .Include(e => e.Department)
            .Include(e => e.Bank)
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id, e.EmployeeCode, e.Name, e.Email, e.Phone,
                e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.Designation,
                e.JoinDate, e.Salary, e.IsActive,
                e.BankId, e.AccountNumber,
                BankName = e.Bank != null ? e.Bank.Name : null,
                e.Gender
            })
            .ToListAsync();

        // Remove duplicates in memory
        var uniqueEmployees = employees
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();

        return Ok(uniqueEmployees);
    }

    /// <summary>
    /// GET /api/employees/{id}
    /// Returns a single employee record including department and bank details.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var emp = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Bank)
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id, e.EmployeeCode, e.Name, e.Email, e.Phone,
                e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.Designation,
                e.JoinDate, e.Salary, e.IsActive,
                e.BankId, e.AccountNumber,
                BankName = e.Bank != null ? e.Bank.Name : null,
                e.Gender
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        return Ok(emp);
    }

    /// <summary>
    /// POST /api/employees
    /// Creates a new employee record.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeRequest req)
    {
        // Validate department exists if provided
        if (req.DepartmentId.HasValue)
        {
            var deptExists = await _db.Departments.AnyAsync(d => d.Id == req.DepartmentId.Value);
            if (!deptExists)
                throw new KeyNotFoundException($"Department with ID {req.DepartmentId} not found.");
        }

        // Validate bank exists if provided
        if (req.BankId.HasValue)
        {
            var bankExists = await _db.BankMasters.AnyAsync(b => b.Id == req.BankId.Value);
            if (!bankExists)
                throw new KeyNotFoundException($"Bank with ID {req.BankId} not found.");
        }

        // Auto-generate employee code - find the highest sequence number from all employee codes
        var allEmployeeCodes = await _db.Employees
            .Where(e => e.EmployeeCode.StartsWith("EMP"))
            .Select(e => e.EmployeeCode)
            .ToListAsync();
        
        int nextSequence = 1;
        if (allEmployeeCodes.Any())
        {
            var sequenceNumbers = allEmployeeCodes
                .Select(code => 
                {
                    if (code.Length > 3 && int.TryParse(code.Substring(3), out int num))
                        return num;
                    return 0;
                })
                .Where(num => num > 0)
                .ToList();
            
            if (sequenceNumbers.Any())
            {
                nextSequence = sequenceNumbers.Max() + 1;
            }
        }
        
        var employeeCode = $"EMP{nextSequence:D6}";

        // Convert JoinDate to UTC - PostgreSQL requires UTC timestamps
        var joinDateUtc = req.JoinDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(req.JoinDate, DateTimeKind.Utc)
            : req.JoinDate.ToUniversalTime();

        var emp = new Employee
        {
            EmployeeCode   = employeeCode,
            Name           = req.Name,
            Email          = req.Email,
            Phone          = req.Phone,
            DepartmentId   = req.DepartmentId,
            Designation    = req.Designation,
            JoinDate       = joinDateUtc,
            Salary         = req.Salary,
            IsActive       = req.IsActive,
            BankId         = req.BankId,
            AccountNumber  = req.AccountNumber,
            Gender         = req.Gender ?? string.Empty
        };

        _db.Employees.Add(emp);
        
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var innerException = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new
            {
                error = "Database update failed",
                message = ex.Message,
                innerException,
                employeeCode,
                stackTrace = ex.StackTrace
            });
        }

        // Auto-create a User account for the employee
        // Username = part of email before '@'  (e.g. "john.doe@company.com" → "john.doe")
        var userCreated = false;
        var userAlreadyExists = false;
        var generatedUsername = emp.Email.Contains('@')
            ? emp.Email.Split('@')[0].ToLower()
            : emp.Email.ToLower();

        var existingUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emp.Email.ToLower()
                               || u.Username.ToLower() == generatedUsername);

        if (existingUser == null)
        {
            try
            {
                var newUser = new User
                {
                    Username           = generatedUsername,
                    Email              = emp.Email,
                    PasswordHash       = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                    Role               = "Employee",
                    IsActive           = true,
                    CreatedAt          = DateTime.UtcNow,
                    MustChangePassword = true   // force password change on first login
                };
                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();
                userCreated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-create user for employee {Email}", emp.Email);
            }
        }
        else
        {
            userAlreadyExists = true;
        }

        await _audit.LogAsync(
            action:      "Created",
            entityType:  "Employee",
            entityId:    emp.Id.ToString(),
            description: $"Created employee {emp.Name} ({employeeCode})" + (userCreated ? $" and auto-created user account ({generatedUsername})" : ""),
            username:    CurrentUser(),
            userId:      CurrentUserId(),
            newValues:   JsonSerializer.Serialize(new { emp.Name, emp.Email, emp.Designation, emp.Salary, emp.IsActive }),
            ipAddress:   HttpContext.Connection.RemoteIpAddress?.ToString());

        return CreatedAtAction(nameof(GetById), new { id = emp.Id }, new {
            message      = "Employee created successfully",
            employeeCode,
            id           = emp.Id,
            userCreated,
            userAlreadyExists,
            username     = userCreated ? generatedUsername : null,
            defaultPassword = userCreated ? "Employee@123" : null
        });
    }

    /// <summary>
    /// PUT /api/employees/{id}
    /// Updates an existing employee record.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeRequest req)
    {
        var emp = await _db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        // Validate department if changed
        if (req.DepartmentId.HasValue)
        {
            var deptExists = await _db.Departments.AnyAsync(d => d.Id == req.DepartmentId.Value);
            if (!deptExists)
                throw new KeyNotFoundException($"Department with ID {req.DepartmentId} not found.");
        }

        // Validate bank if changed
        if (req.BankId.HasValue)
        {
            var bankExists = await _db.BankMasters.AnyAsync(b => b.Id == req.BankId.Value);
            if (!bankExists)
                throw new KeyNotFoundException($"Bank with ID {req.BankId} not found.");
        }

        // Convert JoinDate to UTC - PostgreSQL requires UTC timestamps
        var joinDateUtc = req.JoinDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(req.JoinDate, DateTimeKind.Utc)
            : req.JoinDate.ToUniversalTime();

        var oldSnap = JsonSerializer.Serialize(new { emp.Name, emp.Email, emp.Designation, emp.Salary, emp.IsActive });

        emp.Name           = req.Name;
        emp.Email          = req.Email;
        emp.Phone          = req.Phone;
        emp.DepartmentId   = req.DepartmentId;
        emp.Designation    = req.Designation;
        emp.JoinDate       = joinDateUtc;
        emp.Salary         = req.Salary;
        emp.IsActive       = req.IsActive;
        emp.BankId         = req.BankId;
        emp.AccountNumber  = req.AccountNumber;
        emp.Gender         = req.Gender ?? string.Empty;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            action:      "Updated",
            entityType:  "Employee",
            entityId:    id.ToString(),
            description: $"Updated employee {emp.Name} ({emp.EmployeeCode})",
            username:    CurrentUser(),
            userId:      CurrentUserId(),
            oldValues:   oldSnap,
            newValues:   JsonSerializer.Serialize(new { emp.Name, emp.Email, emp.Designation, emp.Salary, emp.IsActive }),
            ipAddress:   HttpContext.Connection.RemoteIpAddress?.ToString());

        return NoContent();
    }

    /// <summary>
    /// DELETE /api/employees/{id}
    /// Deletes an employee and all related records (cascades).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var emp = await _db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        var empName = emp.Name;
        var empCode = emp.EmployeeCode;
        _db.Employees.Remove(emp);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            action:      "Deleted",
            entityType:  "Employee",
            entityId:    id.ToString(),
            description: $"Deleted employee {empName} ({empCode})",
            username:    CurrentUser(),
            userId:      CurrentUserId(),
            ipAddress:   HttpContext.Connection.RemoteIpAddress?.ToString());

        return NoContent();
    }
}
