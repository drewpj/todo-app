using TodoApp.Api.Models;
using TodoApp.Api.Repositories;
using TaskStatus = TodoApp.Api.Models.TaskStatus;

namespace TodoApp.Api.UnitTests.Fakes;

public class FakeTaskRepository : ITaskRepository
{
    public readonly List<TaskItem> AllTasks = [];
    private int _nextId = 1;

    public Task<TaskItem?> GetByIdAsync(int id) =>
        Task.FromResult(AllTasks.FirstOrDefault(t => t.Id == id));

    public Task<List<TaskItem>> GetByUserAsync(
        int userId, TaskStatus? status, string sortBy, string sortOrder)
    {
        var query = AllTasks.Where(t => t.UserId == userId && !t.IsDeleted).AsQueryable();
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);

        query = (sortBy, sortOrder) switch
        {
            ("dueDate",   "asc")  => query.OrderBy(t => t.DueDate),
            ("dueDate",   "desc") => query.OrderByDescending(t => t.DueDate),
            ("priority",  "asc")  => query.OrderBy(t => t.Priority),
            ("priority",  "desc") => query.OrderByDescending(t => t.Priority),
            ("createdAt", "asc")  => query.OrderBy(t => t.CreatedAt),
            _                     => query.OrderByDescending(t => t.CreatedAt)
        };

        return Task.FromResult(query.ToList());
    }

    public Task<TaskItem> CreateAsync(TaskItem task)
    {
        task.Id = _nextId++;
        AllTasks.Add(task);
        return Task.FromResult(task);
    }

    public Task UpdateAsync(TaskItem task) => Task.CompletedTask;
}
