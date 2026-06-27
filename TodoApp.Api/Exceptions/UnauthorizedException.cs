namespace TodoApp.Api.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized.")
        : base("UNAUTHORIZED", message) { }
}
