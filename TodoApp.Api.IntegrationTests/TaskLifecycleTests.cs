using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TodoApp.Api.DTOs;

namespace TodoApp.Api.IntegrationTests;

public class TaskLifecycleTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<string> RegisterAndLoginAsync()
    {
        var username = $"user_{Guid.NewGuid():N}";
        var reg = await _client.PostAsJsonAsync("/api/auth/register",
            new { username, password = "testpass1" });
        var body = await reg.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        return body!.Token;
    }

    private HttpClient AuthorizedClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task FullLifecycle_RegisterLoginCRUD_Works()
    {
        var token = await RegisterAndLoginAsync();
        var client = AuthorizedClient(token);

        // Create
        var createResp = await client.PostAsJsonAsync("/api/tasks",
            new { title = "Integration task", status = "Todo", priority = "High" });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<TaskResponse>(JsonOpts);
        Assert.Equal("Integration task", created!.Title);

        // List — task appears
        var listResp = await client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var list = await listResp.Content.ReadFromJsonAsync<TaskListResponse>(JsonOpts);
        Assert.Contains(list!.Tasks, t => t.Id == created.Id);

        // Get by id
        var getResp = await client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        // Update — mark Done
        var updateResp = await client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new { title = "Integration task", status = "Done", priority = "High" });
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<TaskResponse>(JsonOpts);
        Assert.Equal("Done", updated!.Status.ToString());

        // Delete (soft)
        var deleteResp = await client.DeleteAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // List — task no longer appears
        var listAfter = await client.GetAsync("/api/tasks");
        var listAfterBody = await listAfter.Content.ReadFromJsonAsync<TaskListResponse>(JsonOpts);
        Assert.DoesNotContain(listAfterBody!.Tasks, t => t.Id == created.Id);
    }

    [Fact]
    public async Task GetTask_AnotherUsersTask_Returns404()
    {
        // User 1 creates a task
        var token1 = await RegisterAndLoginAsync();
        var client1 = AuthorizedClient(token1);
        var createResp = await client1.PostAsJsonAsync("/api/tasks",
            new { title = "Private task", status = "Todo", priority = "Low" });
        var task = await createResp.Content.ReadFromJsonAsync<TaskResponse>(JsonOpts);

        // User 2 tries to read it
        var token2 = await RegisterAndLoginAsync();
        var client2 = AuthorizedClient(token2);
        var getResp = await client2.GetAsync($"/api/tasks/{task!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);

        // Must be 404, not 403 — no leaking that the task exists
        var body = await getResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("NOT_FOUND", doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateTask_EmptyTitle_Returns400WithDetails()
    {
        var token = await RegisterAndLoginAsync();
        var client = AuthorizedClient(token);

        var response = await client.PostAsJsonAsync("/api/tasks",
            new { title = "", status = "Todo", priority = "Medium" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var err = doc.RootElement.GetProperty("error");
        Assert.Equal("VALIDATION_ERROR", err.GetProperty("code").GetString());
        Assert.True(err.GetProperty("details").GetArrayLength() > 0);
    }
}
