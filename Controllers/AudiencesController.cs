using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InternetAccessManager.Api.Data;
using InternetAccessManager.Api.Entities;
using InternetAccessManager.Api.Models.DTOs;
using InternetAccessManager.Api.Models.Requests;
using System.Security.Claims;

namespace InternetAccessManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudiencesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AudiencesController> _logger;

    public AudiencesController(ApplicationDbContext context, ILogger<AudiencesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/audiences
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AudienceDto>>> GetAudiences()
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        IQueryable<Audience> query = _context.Audiences.Where(a => a.IsActive);

        // If teacher, only show assigned audiences
        if (currentUserRole == "Teacher")
        {
            var assignedAudienceIds = await _context.AssignedAudiences
                .Where(aa => aa.UserId == currentUserId)
                .Select(aa => aa.AudienceId)
                .ToListAsync();

            query = query.Where(a => assignedAudienceIds.Contains(a.Id));
        }

        var audiences = await query
            .Select(a => new AudienceDto
            {
                Id = a.Id,
                Name = a.Name,
                Building = a.Building,
                Floor = a.Floor,
                Description = a.Description,
                CurrentStatus = a.CurrentStatus,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                IsActive = a.IsActive,
                ComputersCount = _context.Computers.Count(c => c.AudienceId == a.Id && c.IsActive)
            })
            .ToListAsync();

        return Ok(audiences);
    }

    // GET: api/audiences/5
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<AudienceDetailDto>> GetAudience(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        var audience = await _context.Audiences
            .Where(a => a.Id == id && a.IsActive)
            .FirstOrDefaultAsync();

        if (audience == null)
            return NotFound();

        // Check if teacher has access to this audience
        if (currentUserRole == "Teacher")
        {
            var hasAccess = await _context.AssignedAudiences
                .AnyAsync(aa => aa.UserId == currentUserId && aa.AudienceId == id);

            if (!hasAccess)
                return Forbid();
        }

        var computers = await _context.Computers
            .Where(c => c.AudienceId == id && c.IsActive)
            .Select(c => new ComputerDto
            {
                Id = c.Id,
                Name = c.Name,
                IPAddress = c.IPAddress,
                MACAddress = c.MACAddress,
                AudienceId = c.AudienceId,
                AudienceName = audience.Name,
                CurrentStatus = c.CurrentStatus,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive
            })
            .ToListAsync();

        var audienceDetail = new AudienceDetailDto
        {
            Id = audience.Id,
            Name = audience.Name,
            Building = audience.Building,
            Floor = audience.Floor,
            Description = audience.Description,
            CurrentStatus = audience.CurrentStatus,
            CreatedAt = audience.CreatedAt,
            UpdatedAt = audience.UpdatedAt,
            IsActive = audience.IsActive,
            ComputersCount = computers.Count,
            Computers = computers
        };

        return Ok(audienceDetail);
    }

    // POST: api/audiences
    [HttpPost]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<ActionResult<AudienceDto>> CreateAudience(InternetAccessManager.Api.Models.DTOs.CreateAudienceRequest request)
    {
        var audience = new Audience
        {
            Name = request.Name,
            Building = request.Building,
            Floor = request.Floor,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Audiences.Add(audience);
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "CreateAudience",
            Description = $"Created audience: {audience.Name}",
            AudienceId = audience.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        var audienceDto = new AudienceDto
        {
            Id = audience.Id,
            Name = audience.Name,
            Building = audience.Building,
            Floor = audience.Floor,
            Description = audience.Description,
            CurrentStatus = audience.CurrentStatus,
            CreatedAt = audience.CreatedAt,
            UpdatedAt = audience.UpdatedAt,
            IsActive = audience.IsActive,
            ComputersCount = 0
        };

        return CreatedAtAction(nameof(GetAudience), new { id = audience.Id }, audienceDto);
    }

    // PUT: api/audiences/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<IActionResult> UpdateAudience(int id, InternetAccessManager.Api.Models.DTOs.UpdateAudienceRequest request)
    {
        var audience = await _context.Audiences.FindAsync(id);
        if (audience == null)
            return NotFound();

        if (request.Name != null)
            audience.Name = request.Name;

        if (request.Building != null)
            audience.Building = request.Building;

        if (request.Floor.HasValue)
            audience.Floor = request.Floor;

        if (request.Description != null)
            audience.Description = request.Description;

        if (request.CurrentStatus != null)
            audience.CurrentStatus = request.CurrentStatus;

        if (request.IsActive.HasValue)
            audience.IsActive = request.IsActive.Value;

        audience.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "UpdateAudience",
            Description = $"Updated audience: {audience.Name}",
            AudienceId = audience.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/audiences/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteAudience(int id)
    {
        var audience = await _context.Audiences.FindAsync(id);
        if (audience == null)
            return NotFound();

        // Check if audience has computers
        var hasComputers = await _context.Computers.AnyAsync(c => c.AudienceId == id && c.IsActive);
        if (hasComputers)
            return BadRequest(new { message = "Cannot delete audience that has computers" });

        // Soft delete
        audience.IsActive = false;
        audience.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "DeleteAudience",
            Description = $"Deleted audience: {audience.Name}",
            AudienceId = audience.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/audiences/5/change-status
    [HttpPost("{id}/change-status")]
    [Authorize]
    public async Task<IActionResult> ChangeAudienceStatus(int id, ChangeStatusRequest request)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        var audience = await _context.Audiences.FindAsync(id);
        if (audience == null)
            return NotFound();

        // Check if teacher has access to this audience
        if (currentUserRole == "Teacher")
        {
            var hasAccess = await _context.AssignedAudiences
                .AnyAsync(aa => aa.UserId == currentUserId && aa.AudienceId == id);

            if (!hasAccess)
                return Forbid();

            // Check if system is locked for teachers
            var systemLock = await _context.SystemLocks.FirstOrDefaultAsync();
            if (systemLock?.IsLocked == true)
                return BadRequest(new { message = "System is locked for teachers" });
        }

        var oldStatus = audience.CurrentStatus;
        audience.CurrentStatus = request.Status;
        audience.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Update all computers in this audience
        var computers = await _context.Computers
            .Where(c => c.AudienceId == id && c.IsActive)
            .ToListAsync();

        foreach (var computer in computers)
        {
            computer.CurrentStatus = request.Status;
            computer.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        // Log action
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "ChangeAudienceStatus",
            Description = $"Changed audience status from {oldStatus} to {request.Status}",
            AudienceId = audience.Id,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status changed successfully" });
    }

    // POST: api/audiences/bulk-change-status
    [HttpPost("bulk-change-status")]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<IActionResult> BulkChangeStatus(BulkChangeStatusRequest request)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var audiences = await _context.Audiences
            .Where(a => request.Ids.Contains(a.Id) && a.IsActive)
            .ToListAsync();

        foreach (var audience in audiences)
        {
            audience.CurrentStatus = request.Status;
            audience.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        // Update all computers in these audiences
        var computers = await _context.Computers
            .Where(c => request.Ids.Contains(c.AudienceId) && c.IsActive)
            .ToListAsync();

        foreach (var computer in computers)
        {
            computer.CurrentStatus = request.Status;
            computer.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        // Log action
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "BulkChangeAudienceStatus",
            Description = $"Changed status to {request.Status} for {audiences.Count} audiences",
            NewStatus = request.Status,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Status changed for {audiences.Count} audiences" });
    }
}