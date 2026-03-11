
using Microsoft.EntityFrameworkCore; // ЭТО САМОЕ ГЛАВНОЕ - для FirstOrDefaultAsync, ToListAsync и т.д.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace InternetAccessManager.Api.Models.DTOs;

public class AudienceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Building { get; set; }
    public int? Floor { get; set; }
    public string? Description { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public int ComputersCount { get; set; }
}

public class AudienceDetailDto : AudienceDto
{
    public List<ComputerDto> Computers { get; set; } = new();
}

public class CreateAudienceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Building { get; set; }
    public int? Floor { get; set; }
    public string? Description { get; set; }
}

public class UpdateAudienceRequest
{
    public string? Name { get; set; }
    public string? Building { get; set; }
    public int? Floor { get; set; }
    public string? Description { get; set; }
    public string? CurrentStatus { get; set; }
    public bool? IsActive { get; set; }
}