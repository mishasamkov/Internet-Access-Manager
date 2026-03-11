using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InternetAccessManager.Api.Data;
using InternetAccessManager.Api.Entities;
using InternetAccessManager.Api.Models.DTOs;
using System.Security.Claims;

namespace InternetAccessManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Technician")]
public class SystemLocksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemLocksController> _logger;

    public SystemLocksController(ApplicationDbContext context, ILogger<SystemLocksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/systemlocks/current
    [HttpGet("current")]
    [AllowAnonymous]
    public async Task<ActionResult<SystemLockDto>> GetCurrentLock()
    {
        var systemLock = await _context.SystemLocks.FirstOrDefaultAsync();

        if (systemLock == null)
        {
            // Create default lock if none exists
            systemLock = new SystemLock();
            _context.SystemLocks.Add(systemLock);
            await _context.SaveChangesAsync();
        }

        var lockDto = new SystemLockDto
        {
            Id = systemLock.Id,
            IsLocked = systemLock.IsLocked,
            LockedByUserId = systemLock.LockedByUserId,
            LockedByUsername = systemLock.LockedByUserId.HasValue
                ? await _context.Users
                    .Where(u => u.Id == systemLock.LockedByUserId)
                    .Select(u => u.Username)
                    .FirstOrDefaultAsync()
                : null,
            LockedAt = systemLock.LockedAt,
            Reason = systemLock.Reason
        };

        return Ok(lockDto);
    }

    // POST: api/systemlocks/set
    [HttpPost("set")]
    public async Task<IActionResult> SetLock(SetLockRequest request)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        var systemLock = await _context.SystemLocks.FirstOrDefaultAsync();
        if (systemLock == null)
        {
            systemLock = new SystemLock();
            _context.SystemLocks.Add(systemLock);
        }

        systemLock.IsLocked = request.IsLocked;
        systemLock.LockedByUserId = request.IsLocked ? currentUserId : null;
        systemLock.LockedAt = request.IsLocked ? DateTime.UtcNow : null;
        systemLock.Reason = request.IsLocked ? request.Reason : null;

        await _context.SaveChangesAsync();

        // Log action
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = request.IsLocked ? "LockSystem" : "UnlockSystem",
            Description = request.IsLocked
                ? $"System locked. Reason: {request.Reason}"
                : "System unlocked",
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { message = request.IsLocked ? "System locked" : "System unlocked" });
    }

    // POST: api/systemlocks/toggle
    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleLock([FromBody] string? reason = null)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var systemLock = await _context.SystemLocks.FirstOrDefaultAsync();
        if (systemLock == null)
        {
            systemLock = new SystemLock();
            _context.SystemLocks.Add(systemLock);
        }

        systemLock.IsLocked = !systemLock.IsLocked;
        systemLock.LockedByUserId = systemLock.IsLocked ? currentUserId : null;
        systemLock.LockedAt = systemLock.IsLocked ? DateTime.UtcNow : null;
        systemLock.Reason = systemLock.IsLocked ? reason : null;

        await _context.SaveChangesAsync();

        // Log action
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = systemLock.IsLocked ? "LockSystem" : "UnlockSystem",
            Description = systemLock.IsLocked
                ? $"System toggled to locked. Reason: {reason}"
                : "System toggled to unlocked",
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = systemLock.IsLocked ? "System locked" : "System unlocked",
            isLocked = systemLock.IsLocked
        });
    }
}