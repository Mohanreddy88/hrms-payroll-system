using HrmsApi.Data;
using HrmsApi.Models;

namespace HrmsApi.Services;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        string description,
        string username,
        int? userId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null);
}

public class AuditService : IAuditService
{
    private readonly HrmsDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(HrmsDbContext db, ILogger<AuditService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        string description,
        string username,
        int? userId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null)
    {
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Action      = action,
                EntityType  = entityType,
                EntityId    = entityId,
                Description = description,
                Username    = username,
                UserId      = userId,
                OldValues   = oldValues,
                NewValues   = newValues,
                IpAddress   = ipAddress,
                CreatedAt   = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit failure must never crash the main request
            _logger.LogError(ex, "Failed to write audit log: {Action} {EntityType} {EntityId}", action, entityType, entityId);
        }
    }
}
