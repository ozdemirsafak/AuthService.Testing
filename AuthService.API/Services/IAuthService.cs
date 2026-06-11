using AuthService.API.Models;

namespace AuthService.API.Services;

public interface IAuthService
{
    Task<(bool Success, string Error)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, AuthResponse? Data, string Error)> LoginAsync(LoginRequest request);
}