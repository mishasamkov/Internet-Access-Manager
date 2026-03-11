using System.ComponentModel.DataAnnotations;

namespace InternetAccessManager.Api.Entities;

public class Audience
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Building { get; set; }
    public int? Floor { get; set; }
    public string? Description { get; set; }

    [Required]
    public string CurrentStatus { get; set; } = "FullAccess"; // FullAccess, NoAccess, SafeMode

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}