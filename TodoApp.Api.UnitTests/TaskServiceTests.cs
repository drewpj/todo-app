using TodoApp.Api.DTOs;
using TodoApp.Api.Exceptions;
using TodoApp.Api.Models;
using TodoApp.Api.Services;
using TodoApp.Api.UnitTests.Fakes;
using TaskStatus = TodoApp.Api.Models.TaskStatus;
using TaskPriority = TodoApp.Api.Models.TaskPriority;

namespace TodoApp.Api.UnitTests;

public class TaskServiceTests
{
    private static (TaskService svc, FakeTaskRepository repo) CreateService()
    {
        var repo = new FakeTaskRepository();
        return (new TaskService(repo), repo);
    }

    private static TaskRequest ValidRequest(string title = "Test task") => new()
    {
        Title = title,
        Status = TaskStatus.Todo,
        Priority = TaskPriority.Medium
    };

    // ─── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsTaskWithDefaults()
    {
        var (svc, _) = CreateService();
        var result = await svc.CreateAsync(new TaskRequest { Title = "My task" }, userId: 1);

        Assert.Equal("My task", result.Title);
        Assert.Equal("Todo", result.Status.ToString());
        Assert.Equal("Medium", result.Priority.ToString());
        Assert.True(result.Id > 0);
        Assert.True(result.CreatedAt > DateTime.MinValue);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task Create_EmptyTitle_ThrowsValidationException()
    {
        var (svc, _) = CreateService();
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            svc.CreateAsync(new TaskRequest { Title = "", Status = TaskStatus.Todo, Priority = TaskPriority.Medium }, userId: 1));
        Assert.Equal("VALIDATION_ERROR", ex.Code);
    }

    [Fact]
    public async Task Create_TitleTooLong_ThrowsValidationException()
    {
        var (svc, _) = CreateService();
        await Assert.ThrowsAsync<ValidationException>(() =>
            svc.CreateAsync(new TaskRequest { Title = new string('x', 201), Status = TaskStatus.Todo, Priority = TaskPriority.Medium }, userId: 1));
    }

    [Fact]
    public async Task Create_ExplicitStatusAndPriority_StoredCorrectly()
    {
        var (svc, _) = CreateService();
        var result = await svc.CreateAsync(new TaskRequest
        {
            Title = "High priority", Status = TaskStatus.InProgress, Priority = TaskPriority.High
        }, userId: 1);

        Assert.Equal(TaskStatus.InProgress, result.Status);
        Assert.Equal(TaskPriority.High, result.Priority);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_OwnedTask_ReturnsTask()
    {
        var (svc, _) = CreateService();
        var created = await svc.CreateAsync(ValidRequest(), userId: 42);
        var result = await svc.GetByIdAsync(created.Id, userId: 42);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetById_AnotherUsersTask_ThrowsNotFound()
    {
        var (svc, _) = CreateService();
        var created = await svc.CreateAsync(ValidRequest(), userId: 1);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(created.Id, userId: 2));
    }

    [Fact]
    public async Task GetById_SoftDeletedTask_ThrowsNotFound()
    {
        var (svc, _) = CreateService();
        var created = await svc.CreateAsync(ValidRequest(), userId: 1);
        await svc.DeleteAsync(created.Id, userId: 1);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(created.Id, userId: 1));
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_OwnedTask_UpdatesFieldsAndTimestamp()
    {
        var (svc, repo) = CreateService();
        var created = await svc.CreateAsync(ValidRequest("Original"), userId: 5);
        var originalUpdatedAt = created.UpdatedAt;

        await Task.Delay(1); // ensure UpdatedAt changes
        var result = await svc.UpdateAsync(created.Id,
            new TaskRequest { Title = "Updated", Status = TaskStatus.Done, Priority = TaskPriority.Low },
            userId: 5);

        Assert.Equal("Updated", result.Title);
        Assert.Equal(TaskStatus.Done, result.Status);
        // Verify the underlying entity was mutated
        var entity = repo.AllTasks.First(t => t.Id == created.Id);
        Assert.Equal("Updated", entity.Title);
        Assert.True(entity.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public async Task Update_AnotherUsersTask_ThrowsNotFound()
    {
        var (svc, _) = CreateService();
        var created = await svc.CreateAsync(ValidRequest(), userId: 1);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(created.Id, ValidRequest("x"), userId: 2));
    }

    // ─── Delete ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletesSetsIsDeleted()
    {
        var (svc, repo) = CreateService();
        var created = await svc.CreateAsync(ValidRequest(), userId: 1);
        await svc.DeleteAsync(created.Id, userId: 1);

        var entity = repo.AllTasks.First(t => t.Id == created.Id);
        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public async Task Delete_AnotherUsersTask_ThrowsNotFound()
    {
        var (svc, _) = CreateService();
        var created = await svc.CreateAsync(ValidRequest(), userId: 1);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(created.Id, userId: 2));
    }

    // ─── GetTasks (filter/sort) ───────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_FilterByStatus_ReturnsOnlyMatchingTasks()
    {
        var (svc, _) = CreateService();
        await svc.CreateAsync(new TaskRequest { Title = "A", Status = TaskStatus.Todo, Priority = TaskPriority.Low }, userId: 1);
        await svc.CreateAsync(new TaskRequest { Title = "B", Status = TaskStatus.Done, Priority = TaskPriority.Low }, userId: 1);

        var result = await svc.GetTasksAsync(userId: 1, status: "Todo", sortBy: null, sortOrder: null);
        Assert.Single(result.Tasks);
        Assert.Equal("A", result.Tasks[0].Title);
    }

    [Fact]
    public async Task GetTasks_NoFilter_ReturnsAllNonDeletedTasks()
    {
        var (svc, _) = CreateService();
        var t1 = await svc.CreateAsync(ValidRequest("A"), userId: 1);
        await svc.CreateAsync(ValidRequest("B"), userId: 1);
        await svc.DeleteAsync(t1.Id, userId: 1);

        var result = await svc.GetTasksAsync(userId: 1, status: null, sortBy: null, sortOrder: null);
        Assert.Single(result.Tasks);
        Assert.Equal("B", result.Tasks[0].Title);
    }

    [Fact]
    public async Task GetTasks_InvalidStatus_ThrowsValidationException()
    {
        var (svc, _) = CreateService();
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            svc.GetTasksAsync(userId: 1, status: "NotAStatus", sortBy: null, sortOrder: null));
        Assert.Equal("VALIDATION_ERROR", ex.Code);
    }

    [Fact]
    public async Task GetTasks_InvalidSortBy_ThrowsValidationException()
    {
        var (svc, _) = CreateService();
        await Assert.ThrowsAsync<ValidationException>(() =>
            svc.GetTasksAsync(userId: 1, status: null, sortBy: "badField", sortOrder: null));
    }

    [Fact]
    public async Task GetTasks_InvalidSortOrder_ThrowsValidationException()
    {
        var (svc, _) = CreateService();
        await Assert.ThrowsAsync<ValidationException>(() =>
            svc.GetTasksAsync(userId: 1, status: null, sortBy: null, sortOrder: "sideways"));
    }

    [Fact]
    public async Task GetTasks_DefaultSort_IsCreatedAtDesc()
    {
        var (svc, repo) = CreateService();
        var now = DateTime.UtcNow;
        // Manually seed with specific timestamps
        repo.AllTasks.Add(new TaskItem { Id = 10, UserId = 1, Title = "Older", CreatedAt = now.AddDays(-2), UpdatedAt = now });
        repo.AllTasks.Add(new TaskItem { Id = 11, UserId = 1, Title = "Newer", CreatedAt = now.AddDays(-1), UpdatedAt = now });

        var result = await svc.GetTasksAsync(userId: 1, status: null, sortBy: null, sortOrder: null);
        Assert.Equal("Newer", result.Tasks[0].Title);
        Assert.Equal("Older", result.Tasks[1].Title);
    }
}
