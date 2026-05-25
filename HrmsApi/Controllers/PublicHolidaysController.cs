using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PublicHolidaysController : ControllerBase
{
    private readonly HrmsDbContext _db;

    public PublicHolidaysController(HrmsDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/publicholidays?year=2025 — list all holidays (optionally filtered by year)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? year = null)
    {
        var query = _db.PublicHolidays.AsQueryable();

        if (year.HasValue)
            query = query.Where(h => h.Year == year.Value);

        var holidays = await query
            .OrderBy(h => h.Date)
            .Select(h => new
            {
                h.Id,
                h.Name,
                h.Date,
                h.Year,
                h.IsNational,
                h.State,
                h.Description
            })
            .ToListAsync();

        return Ok(holidays);
    }

    /// <summary>GET /api/publicholidays/{id} — get single holiday</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var holiday = await _db.PublicHolidays.FindAsync(id);
        if (holiday is null) return NotFound(new { message = "Holiday not found." });
        return Ok(holiday);
    }

    /// <summary>POST /api/publicholidays — create holiday (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] PublicHolidayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Holiday name is required." });

        var holiday = new PublicHoliday
        {
            Name        = request.Name.Trim(),
            Date        = request.Date.Date,
            Year        = request.Date.Year,
            IsNational  = request.IsNational,
            State       = request.IsNational ? null : request.State?.Trim(),
            Description = request.Description?.Trim(),
            // CreatedAt not mapped — column does not exist in live DB
        };

        _db.PublicHolidays.Add(holiday);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = holiday.Id }, holiday);
    }

    /// <summary>PUT /api/publicholidays/{id} — update holiday (Admin only)</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] PublicHolidayRequest request)
    {
        var holiday = await _db.PublicHolidays.FindAsync(id);
        if (holiday is null) return NotFound(new { message = "Holiday not found." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Holiday name is required." });

        holiday.Name        = request.Name.Trim();
        holiday.Date        = request.Date.Date;
        holiday.Year        = request.Date.Year;
        holiday.IsNational  = request.IsNational;
        holiday.State       = request.IsNational ? null : request.State?.Trim();
        holiday.Description = request.Description?.Trim();

        await _db.SaveChangesAsync();
        return Ok(holiday);
    }

    /// <summary>DELETE /api/publicholidays/{id} — delete holiday (Admin only)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var holiday = await _db.PublicHolidays.FindAsync(id);
        if (holiday is null) return NotFound(new { message = "Holiday not found." });

        _db.PublicHolidays.Remove(holiday);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Holiday deleted successfully." });
    }

    /// <summary>GET /api/publicholidays/years — list distinct years that have holidays</summary>
    [HttpGet("years")]
    public async Task<IActionResult> GetYears()
    {
        var years = await _db.PublicHolidays
            .Select(h => h.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        // Always include current and next year
        var currentYear = DateTime.Now.Year;
        if (!years.Contains(currentYear))     years.Insert(0, currentYear);
        if (!years.Contains(currentYear + 1)) years.Insert(0, currentYear + 1);

        return Ok(years.OrderByDescending(y => y));
    }
}
