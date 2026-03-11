using System.ComponentModel.DataAnnotations;

namespace InternetAccessManager.Api.Entities;

public class Computer
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string IPAddress { get; set; } = string.Empty;

    public string? MACAddress { get; set; }

    [Required]
    public int AudienceId { get; set; }

    [Required]
    public string CurrentStatus { get; set; } = "FullAccess"; // FullAccess, NoAccess, SafeMode

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}