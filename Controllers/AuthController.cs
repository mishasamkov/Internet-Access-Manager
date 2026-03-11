using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InternetAccessManager.Api.Models.DTOs;
using InternetAccessManager.Api.Services;

namespace InternetAccessManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var response = await _authService.LoginAsync(request, ipAddress);

        if (response == null)
            return Unauthorized(new { message = "Invalid username or password" });

        return Ok(response);
    }
}