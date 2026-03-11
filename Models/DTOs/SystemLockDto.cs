namespace InternetAccessManager.Api.Models.DTOs;

public class SystemLockDto
{
    public int Id { get; set; }
    public bool IsLocked { get; set; }
    public int? LockedByUserId { get; set; }
    public string? LockedByUsername { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? Reason { get; set; }
}

public class SetLockRequest
{
    public bool IsLocked { get; set; }
    public string? Reason { get; set; }
}