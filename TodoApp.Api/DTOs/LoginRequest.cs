using System.ComponentModel.DataAnnotations;

namespace TodoApp.Api.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(200, ErrorMessage = "Password cannot exceed 200 characters.")]
    public string Password { get; set; } = string.Empty;
}
