using HrmsApi.Data;
using HrmsApi.Models;

namespace HrmsApi.Services;

public interface INotificationService
{
    Task NotifyUserAsync(int userId, string title, string message, string type, string? link = null);
    Task NotifyAdminsAsync(string title, string message, string type, string? link = null);
}

public class NotificationService : INotificationService
{
    private readonly HrmsDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(HrmsDbContext db, ILogger<NotificationService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    /// <summary>Send an in-app notification to a specific user by their User.Id</summary>
    public async Task NotifyUserAsync(int userId, string title, string message, string type, string? link = null)
    {
        try
        {
            _db.Notifications.Add(new Notification
            {
                UserId    = userId,
                Title     = title,
                Message   = message,
                Type      = type,
                Link      = link,
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}: {Title}", userId, title);
        }
    }

    /// <summary>Send the same notification to all Admin users</summary>
    public async Task NotifyAdminsAsync(string title, string message, string type, string? link = null)
    {
        try
        {
            var adminIds = _db.Users
                .Where(u => u.Role == "Admin" && u.IsActive)
                .Select(u => u.Id)
                .ToList();

            foreach (var adminId in adminIds)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId    = adminId,
                    Title     = title,
                    Message   = message,
                    Type      = type,
                    Link      = link,
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create admin notifications: {Title}", title);
        }
    }
}
