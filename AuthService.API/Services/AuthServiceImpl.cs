using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.API.Data;
using AuthService.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.API.Services;

public class AuthServiceImpl : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthServiceImpl(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(bool Success, string Error)> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return (false, "Email boş olamaz.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return (false, "Şifre en az 6 karakter olmalıdır.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return (false, "Ad Soyad boş olamaz.");

        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant());
        if (exists)
            return (false, "Bu email adresi zaten kayıtlı.");

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return (true, string.Empty);
    }

    public async Task<(bool Success, AuthResponse? Data, string Error)> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return (false, null, "Email ve şifre zorunludur.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null)
            return (false, null, "Email veya şifre hatalı.");

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
            return (false, null, "Email veya şifre hatalı.");

        var token = GenerateJwt(user);

        return (true, new AuthResponse
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName
        }, string.Empty);
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "super-secret-key-for-dev-only-123456"));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "AuthService",
            audience: _config["Jwt:Audience"] ?? "AuthService",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}