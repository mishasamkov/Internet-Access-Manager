using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InternetAccessManager.Api.Data;
using InternetAccessManager.Api.Entities;
using InternetAccessManager.Api.Models.DTOs;
using InternetAccessManager.Api.Services;
using System.Security.Claims;

namespace InternetAccessManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ILogger<UsersController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        // Check if username or email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

        if (existingUser != null)
            return BadRequest(new { message = "Username or email already exists" });

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "CreateUser",
            Description = $"Created user: {user.Username}",
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
    }

    // PUT: api/users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        if (request.Email != null)
            user.Email = request.Email;

        if (request.Role != null)
            user.Role = request.Role;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        // Log action
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "UpdateUser",
            Description = $"Updated user: {user.Username}",
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        // Don't allow deleting yourself
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == id)
            return BadRequest(new { message = "Cannot delete your own account" });

        // Soft delete
        user.IsActive = false;
        await _context.SaveChangesAsync();

        // Log action
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "DeleteUser",
            Description = $"Deleted user: {user.Username}",
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/users/5/change-password
    [HttpPost("{id}/change-password")]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<IActionResult> ChangePassword(int id, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        // If not admin, verify old password
        if (currentUserRole != "Administrator" && currentUserId == id)
        {
            if (!_passwordService.VerifyPassword(request.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Old password is incorrect" });
        }

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        // Log action
        var log = new ActionLog
        {
            UserId = currentUserId,
            Action = "ChangePassword",
            Description = $"Changed password for user: {user.Username}",
            CreatedAt = DateTime.UtcNow
        };
        _context.ActionLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully" });
    }
}