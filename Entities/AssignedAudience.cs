namespace InternetAccessManager.Api.Entities;

public class AssignedAudience
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AudienceId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public int AssignedByUserId { get; set; }
}