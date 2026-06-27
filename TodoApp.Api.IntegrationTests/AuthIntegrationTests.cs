using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TodoApp.Api.DTOs;

namespace TodoApp.Api.IntegrationTests;

public class AuthIntegrationTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Register_ValidInput_Returns201WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username = $"user_{Guid.NewGuid():N}", password = "testpass1" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        Assert.NotEmpty(body!.Token);
        Assert.NotEmpty(body.User.Username);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns409WithStandardError()
    {
        var username = $"dup_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/auth/register",
            new { username, password = "testpass1" });

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username, password = "testpass2" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("DUPLICATE_USERNAME", doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_EmptyTitle_Returns400WithFieldDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username = "", password = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var err = doc.RootElement.GetProperty("error");
        Assert.Equal("VALIDATION_ERROR", err.GetProperty("code").GetString());
        Assert.True(err.GetProperty("details").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Login_BadPassword_Returns401()
    {
        var username = $"user_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/auth/register",
            new { username, password = "correctpass" });

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username, password = "wrongpass" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TaskEndpoints_NoToken_Return401()
    {
        var response = await _client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
