using TodoApp.Api.Models;
using TaskStatus = TodoApp.Api.Models.TaskStatus;

namespace TodoApp.Api.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(int id);
    Task<List<TaskItem>> GetByUserAsync(int userId, TaskStatus? status, string sortBy, string sortOrder);
    Task<TaskItem> CreateAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
}
