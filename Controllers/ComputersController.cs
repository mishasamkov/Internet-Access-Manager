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
public class ComputersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ComputersController> _logger;

    public ComputersController(ApplicationDbContext context, ILogger<ComputersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/computers
    [HttpGet]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<ActionResult<IEnumerable<ComputerDto>>> GetComputers()
    {
        var computers = await _context.Computers
            .Where(c => c.IsActive)
            .Select(c => new ComputerDto
            {
                Id = c.Id,
                Name = c.Name,
                IPAddress = c.IPAddress,
                MACAddress = c.MACAddress,
                AudienceId = c.AudienceId,
                AudienceName = _context.Audiences
                    .Where(a => a.Id == c.AudienceId)
                    .Select(a => a.Name)
                    .FirstOrDefault() ?? "",
                CurrentStatus = c.CurrentStatus,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive
            })
            .ToListAsync();

        return Ok(computers);
    }

    // GET: api/computers/5
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ComputerDto>> GetComputer(int id)
    {
        var computer = await _context.Computers
            .Where(c => c.Id == id && c.IsActive)
            .Select(c => new ComputerDto
            {
                Id = c.Id,
                Name = c.Name,
                IPAddress = c.IPAddress,
                MACAddress = c.MACAddress,
                AudienceId = c.AudienceId,
                AudienceName = _context.Audiences
                    .Where(a => a.Id == c.AudienceId)
                    .Select(a => a.Name)
                    .FirstOrDefault() ?? "",
                CurrentStatus = c.CurrentStatus,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync();

        if (computer == null)
            return NotFound();

        return Ok(computer);
    }

    // POST: api/computers
    [HttpPost]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<ActionResult<ComputerDto>> CreateComputer(InternetAccessManager.Api.Models.DTOs.CreateComputerRequest request)
    {
        // Check if audience exists
        var audience = await _context.Audiences.FindAsync(request.AudienceId);
        if (audience == null)
            return BadRequest(new { message = "Audience not found" });

        // Check if IP address already exists
        var existingComputer = await _context.Computers
            .FirstOrDefaultAsync(c => c.IPAddress == request.IPAddress && c.IsActive);

        if (existingComputer != null)
            return BadRequest(new { message = "Computer with this IP address already exists" });

        var computer = new Computer
        {
            Name = request.Name,
            IPAddress = request.IPAddress,
            MACAddress = request.MACAddress,
            AudienceId = request.AudienceId,
            CurrentStatus = audience.CurrentStatus, // Inherit status from audience
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Computers.Add(computer);
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "CreateComputer",
            Description = $"Created computer: {computer.Name} in audience {audience.Name}",
            ComputerId = computer.Id,
            AudienceId = computer.AudienceId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        var computerDto = new ComputerDto
        {
            Id = computer.Id,
            Name = computer.Name,
            IPAddress = computer.IPAddress,
            MACAddress = computer.MACAddress,
            AudienceId = computer.AudienceId,
            AudienceName = audience.Name,
            CurrentStatus = computer.CurrentStatus,
            CreatedAt = computer.CreatedAt,
            UpdatedAt = computer.UpdatedAt,
            IsActive = computer.IsActive
        };

        return CreatedAtAction(nameof(GetComputer), new { id = computer.Id }, computerDto);
    }

    // PUT: api/computers/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<IActionResult> UpdateComputer(int id, InternetAccessManager.Api.Models.DTOs.UpdateComputerRequest request)
    {
        var computer = await _context.Computers.FindAsync(id);
        if (computer == null)
            return NotFound();

        if (request.Name != null)
            computer.Name = request.Name;

        if (request.IPAddress != null)
        {
            // Check if new IP address is already taken
            var existingComputer = await _context.Computers
                .FirstOrDefaultAsync(c => c.IPAddress == request.IPAddress && c.Id != id && c.IsActive);

            if (existingComputer != null)
                return BadRequest(new { message = "Computer with this IP address already exists" });

            computer.IPAddress = request.IPAddress;
        }

        if (request.MACAddress != null)
            computer.MACAddress = request.MACAddress;

        if (request.AudienceId.HasValue)
        {
            var audience = await _context.Audiences.FindAsync(request.AudienceId);
            if (audience == null)
                return BadRequest(new { message = "Audience not found" });

            computer.AudienceId = request.AudienceId.Value;
        }

        if (request.CurrentStatus != null)
            computer.CurrentStatus = request.CurrentStatus;

        if (request.IsActive.HasValue)
            computer.IsActive = request.IsActive.Value;

        computer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "UpdateComputer",
            Description = $"Updated computer: {computer.Name}",
            ComputerId = computer.Id,
            AudienceId = computer.AudienceId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/computers/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteComputer(int id)
    {
        var computer = await _context.Computers.FindAsync(id);
        if (computer == null)
            return NotFound();

        // Soft delete
        computer.IsActive = false;
        computer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "DeleteComputer",
            Description = $"Deleted computer: {computer.Name}",
            ComputerId = computer.Id,
            AudienceId = computer.AudienceId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/computers/5/change-status
    [HttpPost("{id}/change-status")]
    [Authorize]
    public async Task<IActionResult> ChangeComputerStatus(int id, ChangeStatusRequest request)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        var computer = await _context.Computers
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (computer == null)
            return NotFound();

        // Check if teacher has access to this computer's audience
        if (currentUserRole == "Teacher")
        {
            var hasAccess = await _context.AssignedAudiences
                .AnyAsync(aa => aa.UserId == currentUserId && aa.AudienceId == computer.AudienceId);

            if (!hasAccess)
                return Forbid();

            // Check if system is locked for teachers
            var systemLock = await _context.SystemLocks.FirstOrDefaultAsync();
            if (systemLock?.IsLocked == true)
                return BadRequest(new { message = "System is locked for teachers" });
        }

        var oldStatus = computer.CurrentStatus;
        computer.CurrentStatus = request.Status;
        computer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log action
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "ChangeComputerStatus",
            Description = $"Changed computer status from {oldStatus} to {request.Status}",
            ComputerId = computer.Id,
            AudienceId = computer.AudienceId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status changed successfully" });
    }

    // GET: api/computers/audience/5
    [HttpGet("audience/{audienceId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ComputerDto>>> GetComputersByAudience(int audienceId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        // Check if teacher has access to this audience
        if (currentUserRole == "Teacher")
        {
            var hasAccess = await _context.AssignedAudiences
                .AnyAsync(aa => aa.UserId == currentUserId && aa.AudienceId == audienceId);

            if (!hasAccess)
                return Forbid();
        }

        var computers = await _context.Computers
            .Where(c => c.AudienceId == audienceId && c.IsActive)
            .Select(c => new ComputerDto
            {
                Id = c.Id,
                Name = c.Name,
                IPAddress = c.IPAddress,
                MACAddress = c.MACAddress,
                AudienceId = c.AudienceId,
                AudienceName = _context.Audiences
                    .Where(a => a.Id == c.AudienceId)
                    .Select(a => a.Name)
                    .FirstOrDefault() ?? "",
                CurrentStatus = c.CurrentStatus,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive
            })
            .ToListAsync();

        return Ok(computers);
    }
}