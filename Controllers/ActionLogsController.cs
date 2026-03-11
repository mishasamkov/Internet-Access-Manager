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
public class ActionLogsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActionLogsController> _logger;

    public ActionLogsController(ApplicationDbContext context, ILogger<ActionLogsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/actionlogs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ActionLogDto>>> GetActionLogs(
        [FromQuery] int? userId,
        [FromQuery] int? audienceId,
        [FromQuery] int? computerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.ActionLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (audienceId.HasValue)
            query = query.Where(l => l.AudienceId == audienceId.Value);

        if (computerId.HasValue)
            query = query.Where(l => l.ComputerId == computerId.Value);

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ActionLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                Username = _context.Users
                    .Where(u => u.Id == l.UserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? "",
                Action = l.Action,
                Description = l.Description,
                AudienceId = l.AudienceId,
                AudienceName = l.AudienceId.HasValue
                    ? _context.Audiences
                        .Where(a => a.Id == l.AudienceId)
                        .Select(a => a.Name)
                        .FirstOrDefault()
                    : null,
                ComputerId = l.ComputerId,
                ComputerName = l.ComputerId.HasValue
                    ? _context.Computers
                        .Where(c => c.Id == l.ComputerId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                    : null,
                OldStatus = l.OldStatus,
                NewStatus = l.NewStatus,
                CreatedAt = l.CreatedAt,
                IPAddress = l.IPAddress
            })
            .ToListAsync();

        Response.Headers.Add("X-Total-Count", totalCount.ToString());
        Response.Headers.Add("X-Total-Pages", Math.Ceiling(totalCount / (double)pageSize).ToString());

        return Ok(logs);
    }

    // GET: api/actionlogs/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ActionLogDto>> GetActionLog(int id)
    {
        var log = await _context.ActionLogs
            .Where(l => l.Id == id)
            .Select(l => new ActionLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                Username = _context.Users
                    .Where(u => u.Id == l.UserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? "",
                Action = l.Action,
                Description = l.Description,
                AudienceId = l.AudienceId,
                AudienceName = l.AudienceId.HasValue
                    ? _context.Audiences
                        .Where(a => a.Id == l.AudienceId)
                        .Select(a => a.Name)
                        .FirstOrDefault()
                    : null,
                ComputerId = l.ComputerId,
                ComputerName = l.ComputerId.HasValue
                    ? _context.Computers
                        .Where(c => c.Id == l.ComputerId)
                        .Select(c => c.Name)
                        .FirstOrDefault()
                    : null,
                OldStatus = l.OldStatus,
                NewStatus = l.NewStatus,
                CreatedAt = l.CreatedAt,
                IPAddress = l.IPAddress
            })
            .FirstOrDefaultAsync();

        if (log == null)
            return NotFound();

        return Ok(log);
    }

    // GET: api/actionlogs/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var stats = new
        {
            TotalLogs = await _context.ActionLogs.CountAsync(),
            TodayLogs = await _context.ActionLogs.CountAsync(l => l.CreatedAt >= today),
            LastWeekLogs = await _context.ActionLogs.CountAsync(l => l.CreatedAt >= weekAgo),
            ActionsByType = await _context.ActionLogs
                .GroupBy(l => l.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToListAsync(),
            TopUsers = await _context.ActionLogs
                .GroupBy(l => l.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Username = _context.Users
                        .Where(u => u.Id == g.Key)
                        .Select(u => u.Username)
                        .FirstOrDefault(),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync()
        };

        return Ok(stats);
    }
}