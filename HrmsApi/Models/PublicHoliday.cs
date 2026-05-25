namespace HrmsApi.Models;

/// <summary>
/// Malaysia public holidays
/// </summary>
public class PublicHoliday
{
    public int      Id          { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public DateTime Date        { get; set; }
    public int      Year        { get; set; }
    public bool     IsNational  { get; set; } = true;
    public string?  State       { get; set; }  // NULL for national, or state name
    public string?  Description { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
}

public class PublicHolidayRequest
{
    public string   Name        { get; set; } = string.Empty;
    public DateTime Date        { get; set; }
    public bool     IsNational  { get; set; } = true;
    public string?  State       { get; set; }
    public string?  Description { get; set; }
}
