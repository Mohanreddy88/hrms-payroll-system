using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly HrmsDbContext _db;

    public DepartmentsController(HrmsDbContext db) => _db = db;

    // ──────────────────────────────────────────────────────────
    // GET: api/departments (active only - for dropdowns)
    // ──────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetActiveDepartments()
    {
        var departments = await _db.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Description,
                d.IsActive
            })
            .ToListAsync();

        return Ok(departments);
    }

    // ──────────────────────────────────────────────────────────
    // GET: api/departments/all (all departments - for admin management)
    // ──────────────────────────────────────────────────────────
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllDepartments()
    {
        var departments = await _db.Departments
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Description,
                d.IsActive,
                d.CreatedAt,
                EmployeeCount = d.Employees.Count(e => e.IsActive)
            })
            .ToListAsync();

        return Ok(departments);
    }

    // ──────────────────────────────────────────────────────────
    // GET: api/departments/{id}
    // ──────────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDepartmentById(int id)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department == null) return NotFound(new { message = "Department not found." });

        return Ok(new
        {
            department.Id,
            department.Name,
            department.Description,
            department.IsActive
        });
    }

    // ──────────────────────────────────────────────────────────
    // POST: api/departments
    // ──────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDepartment([FromBody] DepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Department name is required." });

        // Check for duplicate name
        var exists = await _db.Departments.AnyAsync(d => d.Name.ToLower() == request.Name.ToLower());
        if (exists)
            return BadRequest(new { message = $"Department '{request.Name}' already exists." });

        var department = new Department
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDepartmentById), new { id = department.Id }, new
        {
            department.Id,
            department.Name,
            department.Description,
            department.IsActive
        });
    }

    // ──────────────────────────────────────────────────────────
    // PUT: api/departments/{id}
    // ──────────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] DepartmentRequest request)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department == null) return NotFound(new { message = "Department not found." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Department name is required." });

        // Check for duplicate name (excluding current department)
        var duplicate = await _db.Departments
            .AnyAsync(d => d.Id != id && d.Name.ToLower() == request.Name.ToLower());
        if (duplicate)
            return BadRequest(new { message = $"Department '{request.Name}' already exists." });

        department.Name = request.Name.Trim();
        department.Description = request.Description?.Trim() ?? string.Empty;
        department.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ──────────────────────────────────────────────────────────
    // DELETE: api/departments/{id}
    // ──────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _db.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null) return NotFound(new { message = "Department not found." });

        // Check if department has employees
        if (department.Employees.Any())
            return BadRequest(new { message = "Cannot delete department with assigned employees." });

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

// ── Request DTOs ──────────────────────────────────────────────
public record DepartmentRequest(string Name, string? Description, bool IsActive);
