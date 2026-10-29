namespace Application.Dtos;

public class CreateUserDto
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public string DisplayName { get; set; } = String.Empty;
    public required string PasswordHash { get; set; }
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
