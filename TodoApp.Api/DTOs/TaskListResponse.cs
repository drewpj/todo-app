namespace TodoApp.Api.DTOs;

// Wrapping object leaves room to add pagination metadata later without a breaking change.
public class TaskListResponse
{
    public List<TaskResponse> Tasks { get; set; } = [];
}
