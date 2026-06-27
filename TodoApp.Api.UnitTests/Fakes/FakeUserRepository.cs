using TodoApp.Api.Models;
using TodoApp.Api.Repositories;

namespace TodoApp.Api.UnitTests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];
    private int _nextId = 1;

    public Task<User?> GetByIdAsync(int id) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByUsernameAsync(string username) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Username == username));

    public Task<User> CreateAsync(User user)
    {
        user.Id = _nextId++;
        _users.Add(user);
        return Task.FromResult(user);
    }
}
