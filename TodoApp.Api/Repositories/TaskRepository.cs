using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;
using TodoApp.Api.Models;
using TaskStatus = TodoApp.Api.Models.TaskStatus;

namespace TodoApp.Api.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<TaskItem?> GetByIdAsync(int id) =>
        await db.Tasks.FindAsync(id);

    public async Task<List<TaskItem>> GetByUserAsync(
        int userId, TaskStatus? status, string sortBy, string sortOrder)
    {
        var query = db.Tasks.Where(t => t.UserId == userId && !t.IsDeleted);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        query = (sortBy, sortOrder) switch
        {
            ("dueDate",   "asc")  => query.OrderBy(t => t.DueDate),
            ("dueDate",   "desc") => query.OrderByDescending(t => t.DueDate),
            ("priority",  "asc")  => query.OrderBy(t => t.Priority),
            ("priority",  "desc") => query.OrderByDescending(t => t.Priority),
            ("createdAt", "asc")  => query.OrderBy(t => t.CreatedAt),
            ("createdAt", "desc") => query.OrderByDescending(t => t.CreatedAt),
            ("title",     "asc")  => query.OrderBy(t => t.Title),
            ("title",     "desc") => query.OrderByDescending(t => t.Title),
            _                     => query.OrderByDescending(t => t.CreatedAt)
        };

        return await query.ToListAsync();
    }

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public async Task UpdateAsync(TaskItem task)
    {
        db.Tasks.Update(task);
        await db.SaveChangesAsync();
    }
}
