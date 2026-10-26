namespace Application.Dtos;

public record RegisterUserRequest(string Username, string Password, string? DisplayName = null);

public record LoginRequest(string Username, string Password);
