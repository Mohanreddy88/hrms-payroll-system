using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BanksController : ControllerBase
{
    private readonly HrmsDbContext _db;

    public BanksController(HrmsDbContext db) => _db = db;

    /// <summary>
    /// GET /api/banks
    /// Returns all active banks for use in dropdowns.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var banks = await _db.BankMasters
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name, b.IsActive })
            .ToListAsync();

        return Ok(banks);
    }

    /// <summary>
    /// GET /api/banks/all
    /// Returns ALL banks including inactive (Admin management view).
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllIncludingInactive()
    {
        var banks = await _db.BankMasters
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name, b.IsActive, b.CreatedDate, b.CreatedBy })
            .ToListAsync();

        return Ok(banks);
    }

    /// <summary>
    /// POST /api/banks
    /// Creates a new bank entry (Admin only).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BankMasterRequest req)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "Admin") return Forbid();

        var exists = await _db.BankMasters.AnyAsync(b => b.Name == req.Name);
        if (exists)
            throw new InvalidOperationException($"Bank '{req.Name}' already exists.");

        var bank = new BankMaster
        {
            Name      = req.Name,
            IsActive  = req.IsActive,
            CreatedBy = User.Identity?.Name ?? "system"
        };

        _db.BankMasters.Add(bank);
        await _db.SaveChangesAsync();

        return Ok(new { bank.Id, bank.Name, bank.IsActive });
    }

    /// <summary>
    /// PUT /api/banks/{id}
    /// Updates a bank entry (Admin only).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BankMasterRequest req)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "Admin") return Forbid();

        var bank = await _db.BankMasters.FindAsync(id)
            ?? throw new KeyNotFoundException($"Bank {id} not found.");

        bank.Name        = req.Name;
        bank.IsActive    = req.IsActive;
        bank.UpdatedDate = DateTime.UtcNow;
        bank.UpdatedBy   = User.Identity?.Name ?? "system";

        await _db.SaveChangesAsync();
        return Ok(new { bank.Id, bank.Name, bank.IsActive });
    }

    /// <summary>
    /// DELETE /api/banks/{id}
    /// Soft-deletes a bank by setting IsActive = false (Admin only).
    /// Hard delete is blocked if employees use this bank.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "Admin") return Forbid();

        var bank = await _db.BankMasters.FindAsync(id)
            ?? throw new KeyNotFoundException($"Bank {id} not found.");

        // Prevent deleting if employees are linked to this bank
        var inUse = await _db.Employees.AnyAsync(e => e.BankId == id);
        if (inUse)
            throw new InvalidOperationException("Cannot delete bank — it is assigned to one or more employees.");

        _db.BankMasters.Remove(bank);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
