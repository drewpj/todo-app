using TodoApp.Api.DTOs;
using TodoApp.Api.Exceptions;
using TodoApp.Api.Models;
using TodoApp.Api.Repositories;
using TaskStatus = TodoApp.Api.Models.TaskStatus;

namespace TodoApp.Api.Services;

public class TaskService(ITaskRepository taskRepo) : ITaskService
{
    private static readonly string[] ValidSortBy = ["dueDate", "priority", "createdAt", "title"];
    private static readonly string[] ValidSortOrder = ["asc", "desc"];

    public async Task<TaskListResponse> GetTasksAsync(
        int userId, string? status, string? sortBy, string? sortOrder)
    {
        TaskStatus? parsedStatus = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (!Enum.TryParse<TaskStatus>(status, out var s))
                throw new ValidationException("status",
                    $"Invalid status '{status}'. Valid values: Todo, InProgress, Done.");
            parsedStatus = s;
        }

        var resolvedSortBy = sortBy ?? "createdAt";
        if (!ValidSortBy.Contains(resolvedSortBy))
            throw new ValidationException("sortBy", "sortBy must be one of: createdAt, dueDate, priority, title.");

        var resolvedSortOrder = sortOrder ?? "desc";
        if (!ValidSortOrder.Contains(resolvedSortOrder))
            throw new ValidationException("sortOrder", "sortOrder must be asc or desc.");

        var tasks = await taskRepo.GetByUserAsync(userId, parsedStatus, resolvedSortBy, resolvedSortOrder);
        return new TaskListResponse { Tasks = tasks.Select(ToResponse).ToList() };
    }

    public async Task<TaskResponse> GetByIdAsync(int taskId, int userId)
    {
        var task = await GetOwnedTaskAsync(taskId, userId);
        return ToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(TaskRequest request, int userId)
    {
        ValidateTitle(request.Title);
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now
        };
        await taskRepo.CreateAsync(task);
        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(int taskId, TaskRequest request, int userId)
    {
        ValidateTitle(request.Title);
        var task = await GetOwnedTaskAsync(taskId, userId);

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await taskRepo.UpdateAsync(task);
        return ToResponse(task);
    }

    public async Task DeleteAsync(int taskId, int userId)
    {
        var task = await GetOwnedTaskAsync(taskId, userId);
        task.IsDeleted = true;
        task.UpdatedAt = DateTime.UtcNow;
        await taskRepo.UpdateAsync(task);
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("title", "Title is required.");
        if (title.Length > 200)
            throw new ValidationException("title", "Title cannot exceed 200 characters.");
    }

    // Returns 404 whether the task doesn't exist, is soft-deleted, or belongs to another user,
    // so the API never reveals the existence of another user's data.
    private async Task<TaskItem> GetOwnedTaskAsync(int taskId, int userId)
    {
        var task = await taskRepo.GetByIdAsync(taskId);
        if (task is null || task.IsDeleted || task.UserId != userId)
            throw new NotFoundException();
        return task;
    }

    private static TaskResponse ToResponse(TaskItem t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        DueDate = t.DueDate,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
