using AuthService.API.Data;
using AuthService.API.Models;
using AuthService.API.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuthService.UnitTests;

public class AuthServiceTests
{
    private static (AuthServiceImpl service, AppDbContext db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-minimum-32-characters-long!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        var service = new AuthServiceImpl(db, config);
        return (service, db);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldSucceed()
    {
        var (sut, _) = CreateSut();
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123",
            FullName = "Test Kullanıcı"
        };

        var (success, error) = await sut.RegisterAsync(request);

        success.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldFail()
    {
        var (sut, _) = CreateSut();
        var request = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "Password123",
            FullName = "Test Kullanıcı"
        };

        await sut.RegisterAsync(request);
        var (success, error) = await sut.RegisterAsync(request);

        success.Should().BeFalse();
        error.Should().Contain("zaten kayıtlı");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_WithEmptyEmail_ShouldFail(string email)
    {
        var (sut, _) = CreateSut();
        var request = new RegisterRequest
        {
            Email = email,
            Password = "Password123",
            FullName = "Test Kullanıcı"
        };

        var (success, error) = await sut.RegisterAsync(request);

        success.Should().BeFalse();
        error.Should().Contain("Email boş olamaz");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abc")]
    [InlineData("")]
    public async Task Register_WithShortPassword_ShouldFail(string password)
    {
        var (sut, _) = CreateSut();
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = password,
            FullName = "Test Kullanıcı"
        };

        var (success, error) = await sut.RegisterAsync(request);

        success.Should().BeFalse();
        error.Should().Contain("6 karakter");
    }

    [Fact]
    public async Task Register_ShouldSaveEmailAsLowerCase()
    {
        var (sut, db) = CreateSut();
        var request = new RegisterRequest
        {
            Email = "TEST@EXAMPLE.COM",
            Password = "Password123",
            FullName = "Test Kullanıcı"
        };

        await sut.RegisterAsync(request);

        var user = await db.Users.FirstAsync();
        user.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Register_ShouldHashPassword_NotStoreInPlainText()
    {
        var (sut, db) = CreateSut();
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123",
            FullName = "Test Kullanıcı"
        };

        await sut.RegisterAsync(request);

        var user = await db.Users.FirstAsync();
        user.PasswordHash.Should().NotBe("Password123");
        user.PasswordHash.Should().StartWith("$2");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var (sut, _) = CreateSut();
        await sut.RegisterAsync(new RegisterRequest
        {
            Email = "login@example.com",
            Password = "Password123",
            FullName = "Login Test"
        });

        var (success, data, error) = await sut.LoginAsync(new LoginRequest
        {
            Email = "login@example.com",
            Password = "Password123"
        });

        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldFail()
    {
        var (sut, _) = CreateSut();
        await sut.RegisterAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = "CorrectPassword123",
            FullName = "Test Kullanıcı"
        });

        var (success, data, error) = await sut.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        });

        success.Should().BeFalse();
        data.Should().BeNull();
        error.Should().Contain("hatalı");
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldFail()
    {
        var (sut, _) = CreateSut();

        var (success, data, error) = await sut.LoginAsync(new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "Password123"
        });

        success.Should().BeFalse();
        data.Should().BeNull();
        error.Should().Contain("hatalı");
    }

    [Fact]
    public async Task Login_ShouldBeCaseInsensitiveForEmail()
    {
        var (sut, _) = CreateSut();

        await sut.RegisterAsync(new RegisterRequest
        {
            Email = "casesensitive@example.com",
            Password = "Password123",
            FullName = "Test Kullanıcı"
        });

        var (success, data, error) = await sut.LoginAsync(new LoginRequest
        {
            Email = "CASESENSITIVE@EXAMPLE.COM",
            Password = "Password123"
        });

        success.Should().BeTrue();
        data.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_ReturnedToken_ShouldBeValidJwt()
    {
        var (sut, _) = CreateSut();
        await sut.RegisterAsync(new RegisterRequest
        {
            Email = "jwt@example.com",
            Password = "Password123",
            FullName = "JWT Test"
        });

        var (_, data, _) = await sut.LoginAsync(new LoginRequest
        {
            Email = "jwt@example.com",
            Password = "Password123"
        });

        var parts = data!.Token.Split('.');
        parts.Should().HaveCount(3);
    }
}