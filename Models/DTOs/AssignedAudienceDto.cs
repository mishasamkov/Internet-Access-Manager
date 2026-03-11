namespace InternetAccessManager.Api.Models.DTOs;

public class AssignedAudienceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int AudienceId { get; set; }
    public string AudienceName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public int AssignedByUserId { get; set; }
    public string AssignedByUsername { get; set; } = string.Empty;
}

public class CreateAssignedAudienceRequest
{
    public int UserId { get; set; }
    public int AudienceId { get; set; }
}