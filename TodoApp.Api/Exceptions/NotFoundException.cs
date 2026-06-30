namespace TodoApp.Api.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message = "Resource not found.")
        : base("NOT_FOUND", message) { }
}
