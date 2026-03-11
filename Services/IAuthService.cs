using InternetAccessManager.Api.Data;
using InternetAccessManager.Api.Entities;
using InternetAccessManager.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InternetAccessManager.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string ipAddress);
    string GenerateJwtToken(User user);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IPasswordService passwordService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string ipAddress)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Login failed for username: {Username}", request.Username);
                return null;
            }

            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for username: {Username}", request.Username);
                return null;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log action
            var log = new ActionLog
            {
                UserId = user.Id,
                Action = "Login",
                Description = "User logged in successfully",
                IPAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };
            _context.ActionLogs.Add(log);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return null;
        }
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"] ?? "your-secret-key-minimum-32-characters-long");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}