using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly HrmsDbContext _db;

    public AttendanceController(HrmsDbContext db) => _db = db;

    /// <summary>GET /api/attendance?date=yyyy-MM-dd — get attendance by date</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? date)
    {
        var query = _db.Attendances.Include(a => a.Employee).AsQueryable();

        if (DateTime.TryParse(date, out var parsedDate))
            query = query.Where(a => a.Date.Date == parsedDate.Date);

        var records = await query
            .OrderBy(a => a.Employee.Name)
            .Select(a => new
            {
                a.Id, a.EmployeeId,
                EmployeeName = a.Employee.Name,
                Date = a.Date,
                a.Status,
                a.CheckIn,
                a.CheckOut,
                a.WorkHours,
                a.Remarks
            })
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>POST /api/attendance — mark attendance</summary>
    [HttpPost]
    public async Task<IActionResult> Mark([FromBody] AttendanceRequest req)
    {
        var empExists = await _db.Employees.AnyAsync(e => e.Id == req.EmployeeId);
        if (!empExists) throw new KeyNotFoundException($"Employee {req.EmployeeId} not found.");

        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == req.EmployeeId && a.Date.Date == req.Date.Date);

        if (existing is not null)
            throw new InvalidOperationException("Attendance already marked for this employee on this date.");

        // Auto-calculate work hours if CheckIn and CheckOut provided
        decimal workHours = 0;
        if (req.CheckIn.HasValue && req.CheckOut.HasValue && req.CheckOut > req.CheckIn)
        {
            var duration = req.CheckOut.Value - req.CheckIn.Value;
            workHours = (decimal)duration.TotalHours;
        }

        var attendance = new Attendance
        {
            EmployeeId = req.EmployeeId,
            Date       = req.Date,
            Status     = req.Status,
            CheckIn    = req.CheckIn,
            CheckOut   = req.CheckOut,
            WorkHours  = workHours,
            Remarks    = req.Remarks
        };

        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync();

        return Ok(attendance);
    }

    /// <summary>PUT /api/attendance/{id} — update attendance record</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AttendanceRequest req)
    {
        var record = await _db.Attendances.FindAsync(id)
            ?? throw new KeyNotFoundException($"Attendance record {id} not found.");

        record.Status   = req.Status;
        record.CheckIn  = req.CheckIn;
        record.CheckOut = req.CheckOut;
        record.Remarks  = req.Remarks;

        // Auto-calculate work hours if CheckIn and CheckOut provided
        if (req.CheckIn.HasValue && req.CheckOut.HasValue && req.CheckOut > req.CheckIn)
        {
            var duration = req.CheckOut.Value - req.CheckIn.Value;
            record.WorkHours = (decimal)duration.TotalHours;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>DELETE /api/attendance/{id} — delete attendance record</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.Attendances.FindAsync(id)
            ?? throw new KeyNotFoundException($"Attendance record {id} not found.");

        _db.Attendances.Remove(record);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
