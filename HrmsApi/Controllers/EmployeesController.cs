using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly HrmsDbContext _db;

    public EmployeesController(HrmsDbContext db) => _db = db;

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
                e.Id, e.Name, e.Email, e.Phone,
                e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.Designation,
                e.JoinDate, e.Salary, e.IsActive,
                e.IcPassport, e.TaxNumber,
                e.BankId, e.AccountNumber,
                BankName = e.Bank != null ? e.Bank.Name : null,
                e.ProfilePicture
            })
            .ToListAsync();

        return Ok(employees);
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
                e.Id, e.Name, e.Email, e.Phone,
                e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.Designation,
                e.JoinDate, e.Salary, e.IsActive,
                e.IcPassport, e.TaxNumber,
                e.BankId, e.AccountNumber,
                BankName = e.Bank != null ? e.Bank.Name : null,
                e.ProfilePicture
            })
            .ToListAsync();

        return Ok(employees);
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
                e.Id, e.Name, e.Email, e.Phone,
                e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.Designation,
                e.JoinDate, e.Salary, e.IsActive,
                e.IcPassport, e.TaxNumber,
                e.BankId, e.AccountNumber,
                BankName = e.Bank != null ? e.Bank.Name : null,
                e.ProfilePicture
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

        var emp = new Employee
        {
            Name           = req.Name,
            Email          = req.Email,
            Phone          = req.Phone,
            DepartmentId   = req.DepartmentId,
            Designation    = req.Designation,
            JoinDate       = req.JoinDate,
            Salary         = req.Salary,
            IsActive       = req.IsActive,
            IcPassport     = req.IcPassport,
            TaxNumber      = req.TaxNumber,
            BankId         = req.BankId,
            AccountNumber  = req.AccountNumber,
            ProfilePicture = req.ProfilePicture ?? string.Empty
        };

        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
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

        emp.Name           = req.Name;
        emp.Email          = req.Email;
        emp.Phone          = req.Phone;
        emp.DepartmentId   = req.DepartmentId;
        emp.Designation    = req.Designation;
        emp.JoinDate       = req.JoinDate;
        emp.Salary         = req.Salary;
        emp.IsActive       = req.IsActive;
        emp.IcPassport     = req.IcPassport;
        emp.TaxNumber      = req.TaxNumber;
        emp.BankId         = req.BankId;
        emp.AccountNumber  = req.AccountNumber;
        emp.ProfilePicture = req.ProfilePicture ?? string.Empty;

        await _db.SaveChangesAsync();
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

        _db.Employees.Remove(emp);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
