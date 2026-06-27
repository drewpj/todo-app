using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TodoApp.Api.DTOs;
using TodoApp.Api.Exceptions;
using TodoApp.Api.Models;
using TodoApp.Api.Services;
using TodoApp.Api.UnitTests.Fakes;

namespace TodoApp.Api.UnitTests;

public class AuthServiceTests
{
    private static AuthService CreateService(FakeUserRepository? repo = null)
    {
        var fakeRepo = repo ?? new FakeUserRepository();
        var hasher = new PasswordHasher<User>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "unit-test-secret-at-least-32-characters-long!!",
                ["Jwt:Issuer"] = "TodoApp",
                ["Jwt:Audience"] = "TodoApp"
            })
            .Build();
        return new AuthService(fakeRepo, hasher, config);
    }

    [Fact]
    public async Task Register_ValidCredentials_ReturnsToken()
    {
        var svc = CreateService();
        var result = await svc.RegisterAsync(new RegisterRequest { Username = "alice", Password = "password123" });
        Assert.NotEmpty(result.Token);
        Assert.Equal("alice", result.User.Username);
        Assert.True(result.User.Id > 0);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ThrowsConflict()
    {
        var repo = new FakeUserRepository();
        var svc = CreateService(repo);
        await svc.RegisterAsync(new RegisterRequest { Username = "alice", Password = "password123" });

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.RegisterAsync(new RegisterRequest { Username = "alice", Password = "different" }));
        Assert.Equal("DUPLICATE_USERNAME", ex.Code);
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsToken()
    {
        var svc = CreateService();
        await svc.RegisterAsync(new RegisterRequest { Username = "bob", Password = "mypassword" });

        var result = await svc.LoginAsync(new LoginRequest { Username = "bob", Password = "mypassword" });
        Assert.NotEmpty(result.Token);
        Assert.Equal("bob", result.User.Username);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        var svc = CreateService();
        await svc.RegisterAsync(new RegisterRequest { Username = "bob", Password = "mypassword" });

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            svc.LoginAsync(new LoginRequest { Username = "bob", Password = "wrongpassword" }));
        Assert.Equal("UNAUTHORIZED", ex.Code);
    }

    [Fact]
    public async Task Login_NonExistentUsername_ThrowsUnauthorized()
    {
        var svc = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            svc.LoginAsync(new LoginRequest { Username = "nobody", Password = "whatever" }));
    }

    [Fact]
    public async Task GetCurrentUser_ValidId_ReturnsUser()
    {
        var svc = CreateService();
        var registered = await svc.RegisterAsync(new RegisterRequest { Username = "carol", Password = "pass1234" });

        var user = await svc.GetCurrentUserAsync(registered.User.Id);
        Assert.Equal("carol", user.Username);
    }

    [Fact]
    public async Task GetCurrentUser_UnknownId_ThrowsNotFound()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetCurrentUserAsync(999));
    }
}
