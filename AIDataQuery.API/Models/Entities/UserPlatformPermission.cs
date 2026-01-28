namespace AIDataQuery.API.Models.Entities;

public class UserPlatformPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PlatformCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Platform? Platform { get; set; }
}
