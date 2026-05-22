using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly HrmsDbContext _db;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(HrmsDbContext db, ILogger<NotificationController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/notification - Get all notifications for current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var query = _db.Notifications
            .Where(n => n.UserId == user.Id);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.Link,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    /// <summary>
    /// GET /api/notification/unread-count - Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var count = await _db.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .CountAsync();

        return Ok(new { count });
    }

    /// <summary>
    /// POST /api/notification/{id}/mark-read - Mark notification as read
    /// </summary>
    [HttpPost("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

        if (notification == null)
            return NotFound(new { message = "Notification not found" });

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Notification marked as read" });
    }

    /// <summary>
    /// POST /api/notification/mark-all-read - Mark all notifications as read
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var unreadNotifications = await _db.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = $"{unreadNotifications.Count} notifications marked as read" });
    }

    /// <summary>
    /// POST /api/notification - Create a notification (Admin/System only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Link = request.Link,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Notification created", notificationId = notification.Id });
    }

    /// <summary>
    /// DELETE /api/notification/{id} - Delete a notification
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new { message = "User not authenticated" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

        if (notification == null)
            return NotFound(new { message = "Notification not found" });

        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Notification deleted" });
    }
}

public class CreateNotificationRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Link { get; set; }
}
