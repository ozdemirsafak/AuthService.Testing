using AuthService.API.Models;
using AuthService.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, error) = await _authService.RegisterAsync(request);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { message = "Kayıt başarılı." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, data, error) = await _authService.LoginAsync(request);

        if (!success)
            return Unauthorized(new { message = error });

        return Ok(data);
    }
}