using TodoApp.Api.DTOs;

namespace TodoApp.Api.Exceptions;

public class ValidationException : AppException
{
    public IReadOnlyList<FieldError> Details { get; }

    public ValidationException(IReadOnlyList<FieldError> details)
        : base("VALIDATION_ERROR", "One or more fields are invalid.")
    {
        Details = details;
    }

    public ValidationException(string field, string message)
        : this([new FieldError { Field = field, Message = message }]) { }
}
