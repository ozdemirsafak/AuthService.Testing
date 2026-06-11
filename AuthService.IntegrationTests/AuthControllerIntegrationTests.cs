using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthService.API.Models;
using FluentAssertions;

namespace AuthService.IntegrationTests;

public class AuthControllerIntegrationTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = "integration@test.com",
            Password = "Password123",
            FullName = "Integration Test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var request = new RegisterRequest
        {
            Email = "duplicate.integration@test.com",
            Password = "Password123",
            FullName = "Test Kullanıcı"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Contain("zaten kayıtlı");
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = "shortpass@test.com",
            Password = "123",
            FullName = "Test Kullanıcı"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var email = "logintest@integration.com";
        var password = "Password123";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            FullName = "Login Integration"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
        body.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = "wrongpass@integration.com",
            Password = "CorrectPassword123",
            FullName = "Test Kullanıcı"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "wrongpass@integration.com",
            Password = "WrongPassword"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "nobody@integration.com",
            Password = "Password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ThenLogin_FullFlow_ShouldWork()
    {
        var email = "fullflow@integration.com";
        var password = "SecurePassword123";
        var fullName = "Full Flow Test";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            FullName = fullName
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authData.Should().NotBeNull();
        authData!.Token.Should().NotBeNullOrEmpty();
        authData.Email.Should().Be(email);
        authData.FullName.Should().Be(fullName);
    }
}