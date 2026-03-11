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
[Authorize(Roles = "Administrator")]
public class AssignedAudiencesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssignedAudiencesController> _logger;

    public AssignedAudiencesController(ApplicationDbContext context, ILogger<AssignedAudiencesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/assignedaudiences
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssignedAudienceDto>>> GetAssignedAudiences()
    {
        var assignments = await _context.AssignedAudiences
            .Select(aa => new AssignedAudienceDto
            {
                Id = aa.Id,
                UserId = aa.UserId,
                Username = _context.Users
                    .Where(u => u.Id == aa.UserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? "",
                AudienceId = aa.AudienceId,
                AudienceName = _context.Audiences
                    .Where(a => a.Id == aa.AudienceId)
                    .Select(a => a.Name)
                    .FirstOrDefault() ?? "",
                AssignedAt = aa.AssignedAt,
                AssignedByUserId = aa.AssignedByUserId,
                AssignedByUsername = _context.Users
                    .Where(u => u.Id == aa.AssignedByUserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? ""
            })
            .ToListAsync();

        return Ok(assignments);
    }

    // GET: api/assignedaudiences/user/5
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<AssignedAudienceDto>>> GetAssignmentsByUser(int userId)
    {
        var assignments = await _context.AssignedAudiences
            .Where(aa => aa.UserId == userId)
            .Select(aa => new AssignedAudienceDto
            {
                Id = aa.Id,
                UserId = aa.UserId,
                Username = _context.Users
                    .Where(u => u.Id == aa.UserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? "",
                AudienceId = aa.AudienceId,
                AudienceName = _context.Audiences
                    .Where(a => a.Id == aa.AudienceId)
                    .Select(a => a.Name)
                    .FirstOrDefault() ?? "",
                AssignedAt = aa.AssignedAt,
                AssignedByUserId = aa.AssignedByUserId,
                AssignedByUsername = _context.Users
                    .Where(u => u.Id == aa.AssignedByUserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? ""
            })
            .ToListAsync();

        return Ok(assignments);
    }

    // GET: api/assignedaudiences/audience/5
    [HttpGet("audience/{audienceId}")]
    public async Task<ActionResult<IEnumerable<AssignedAudienceDto>>> GetAssignmentsByAudience(int audienceId)
    {
        var assignments = await _context.AssignedAudiences
            .Where(aa => aa.AudienceId == audienceId)
            .Select(aa => new AssignedAudienceDto
            {
                Id = aa.Id,
                UserId = aa.UserId,
                Username = _context.Users
                    .Where(u => u.Id == aa.UserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? "",
                AudienceId = aa.AudienceId,
                AudienceName = _context.Audiences
                    .Where(a => a.Id == aa.AudienceId)
                    .Select(a => a.Name)
                    .FirstOrDefault() ?? "",
                AssignedAt = aa.AssignedAt,
                AssignedByUserId = aa.AssignedByUserId,
                AssignedByUsername = _context.Users
                    .Where(u => u.Id == aa.AssignedByUserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? ""
            })
            .ToListAsync();

        return Ok(assignments);
    }

    // POST: api/assignedaudiences
    [HttpPost]
    public async Task<ActionResult<AssignedAudienceDto>> CreateAssignment(CreateAssignedAudienceRequest request)
    {
        // Check if user exists and is a teacher
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
            return BadRequest(new { message = "User not found" });

        if (user.Role != "Teacher")
            return BadRequest(new { message = "User is not a teacher" });

        // Check if audience exists
        var audience = await _context.Audiences.FindAsync(request.AudienceId);
        if (audience == null)
            return BadRequest(new { message = "Audience not found" });

        // Check if assignment already exists
        var existingAssignment = await _context.AssignedAudiences
            .FirstOrDefaultAsync(aa => aa.UserId == request.UserId && aa.AudienceId == request.AudienceId);

        if (existingAssignment != null)
            return BadRequest(new { message = "Assignment already exists" });

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var assignment = new AssignedAudience
        {
            UserId = request.UserId,
            AudienceId = request.AudienceId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = currentUserId
        };

        _context.AssignedAudiences.Add(assignment);
        await _context.SaveChangesAsync();

        // Log action
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "CreateAssignment",
            Description = $"Assigned audience {audience.Name} to teacher {user.Username}",
            AudienceId = audience.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        var assignmentDto = new AssignedAudienceDto
        {
            Id = assignment.Id,
            UserId = assignment.UserId,
            Username = user.Username,
            AudienceId = assignment.AudienceId,
            AudienceName = audience.Name,
            AssignedAt = assignment.AssignedAt,
            AssignedByUserId = assignment.AssignedByUserId,
            AssignedByUsername = _context.Users
                .Where(u => u.Id == assignment.AssignedByUserId)
                .Select(u => u.Username)
                .FirstOrDefault() ?? ""
        };

        return CreatedAtAction(nameof(GetAssignmentsByUser), new { userId = assignment.UserId }, assignmentDto);
    }

    // DELETE: api/assignedaudiences/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        var assignment = await _context.AssignedAudiences
            .FirstOrDefaultAsync(aa => aa.Id == id);

        if (assignment == null)
            return NotFound();

        var audience = await _context.Audiences.FindAsync(assignment.AudienceId);
        var user = await _context.Users.FindAsync(assignment.UserId);

        _context.AssignedAudiences.Remove(assignment);
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "DeleteAssignment",
            Description = $"Removed audience {(audience?.Name ?? assignment.AudienceId.ToString())} from teacher {(user?.Username ?? assignment.UserId.ToString())}",
            AudienceId = assignment.AudienceId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/assignedaudiences/user/5/audience/5
    [HttpDelete("user/{userId}/audience/{audienceId}")]
    public async Task<IActionResult> DeleteAssignmentByUserAndAudience(int userId, int audienceId)
    {
        var assignment = await _context.AssignedAudiences
            .FirstOrDefaultAsync(aa => aa.UserId == userId && aa.AudienceId == audienceId);

        if (assignment == null)
            return NotFound();

        var audience = await _context.Audiences.FindAsync(audienceId);
        var user = await _context.Users.FindAsync(userId);

        _context.AssignedAudiences.Remove(assignment);
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "DeleteAssignment",
            Description = $"Removed audience {(audience?.Name ?? audienceId.ToString())} from teacher {(user?.Username ?? userId.ToString())}",
            AudienceId = audienceId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}