namespace InternetAccessManager.Api.Entities;

public class ActionLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AudienceId { get; set; }
    public int? ComputerId { get; set; }
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IPAddress { get; set; }
}