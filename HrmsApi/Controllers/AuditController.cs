using HrmsApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly HrmsDbContext _db;

    public AuditController(HrmsDbContext db) => _db = db;

    /// <summary>
    /// GET /api/audit?entityType=Employee&entityId=5&username=admin&from=2026-01-01&to=2026-12-31&page=1&pageSize=50
    /// Returns paginated audit logs with optional filters. Admin only.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string?   entityType = null,
        [FromQuery] string?   entityId   = null,
        [FromQuery] string?   username   = null,
        [FromQuery] string?   action     = null,
        [FromQuery] DateTime? from       = null,
        [FromQuery] DateTime? to         = null,
        [FromQuery] int       page       = 1,
        [FromQuery] int       pageSize   = 50)
    {
        pageSize = Math.Min(pageSize, 200); // hard cap

        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(a => a.EntityId == entityId);

        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(a => a.Username.Contains(username));

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value.AddDays(1));

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Username,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Description,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            logs
        });
    }

    /// <summary>
    /// GET /api/audit/entity/{entityType}/{entityId}
    /// Returns all audit logs for a specific record (e.g. Employee #5). Admin only.
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetEntityHistory(string entityType, string entityId)
    {
        var logs = await _db.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Username,
                a.Action,
                a.Description,
                a.OldValues,
                a.NewValues,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(logs);
    }

    /// <summary>
    /// GET /api/audit/summary
    /// Returns action counts grouped by entity type for the last 30 days. Admin only.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var since = DateTime.UtcNow.AddDays(-30);

        var byEntity = await _db.AuditLogs
            .Where(a => a.CreatedAt >= since)
            .GroupBy(a => new { a.EntityType, a.Action })
            .Select(g => new
            {
                g.Key.EntityType,
                g.Key.Action,
                count = g.Count()
            })
            .OrderBy(x => x.EntityType)
            .ThenBy(x => x.Action)
            .ToListAsync();

        var byUser = await _db.AuditLogs
            .Where(a => a.CreatedAt >= since)
            .GroupBy(a => a.Username)
            .Select(g => new { username = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(10)
            .ToListAsync();

        return Ok(new { since, byEntity, topUsers = byUser });
    }
}
