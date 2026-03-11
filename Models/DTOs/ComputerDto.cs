namespace InternetAccessManager.Api.Models.DTOs;

public class ComputerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string? MACAddress { get; set; }
    public int AudienceId { get; set; }
    public string AudienceName { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class CreateComputerRequest
{
    public string Name { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string? MACAddress { get; set; }
    public int AudienceId { get; set; }
}

public class UpdateComputerRequest
{
    public string? Name { get; set; }
    public string? IPAddress { get; set; }
    public string? MACAddress { get; set; }
    public int? AudienceId { get; set; }
    public string? CurrentStatus { get; set; }
    public bool? IsActive { get; set; }
}