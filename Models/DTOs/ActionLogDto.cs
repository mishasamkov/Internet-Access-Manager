namespace InternetAccessManager.Api.Models.DTOs;

public class ActionLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AudienceId { get; set; }
    public string? AudienceName { get; set; }
    public int? ComputerId { get; set; }
    public string? ComputerName { get; set; }
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IPAddress { get; set; }
}