namespace Application.Dtos;

public class RegisterUserRequest()
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? DisplayName { get; set; }
}

public class LoginRequest()
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
