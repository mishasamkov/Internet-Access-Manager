namespace InternetAccessManager.Api.Entities;

public class SystemLock
{
    public int Id { get; set; }
    public bool IsLocked { get; set; } = false;
    public int? LockedByUserId { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? Reason { get; set; }
}