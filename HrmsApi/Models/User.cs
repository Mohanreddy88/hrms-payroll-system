namespace HrmsApi.Models;

/// <summary>
/// System user account — used for login and role-based access.
/// </summary>
public class User
{
    public int      Id           { get; set; }
    public string   Username     { get; set; } = string.Empty;
    public string   PasswordHash { get; set; } = string.Empty;
    public string   Role         { get; set; } = "Admin";
    public bool     IsActive     { get; set; } = true;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
}
