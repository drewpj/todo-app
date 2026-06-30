using TodoApp.Api.DTOs;

namespace TodoApp.Api.Services;

public interface ITaskService
{
    Task<TaskListResponse> GetTasksAsync(int userId, string? status, string? sortBy, string? sortOrder);
    Task<TaskResponse> GetByIdAsync(int taskId, int userId);
    Task<TaskResponse> CreateAsync(TaskRequest request, int userId);
    Task<TaskResponse> UpdateAsync(int taskId, TaskRequest request, int userId);
    Task DeleteAsync(int taskId, int userId);
}
