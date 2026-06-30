namespace TodoApp.Api.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string code, string message)
        : base(code, message) { }
}
