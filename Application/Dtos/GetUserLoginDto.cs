namespace Application.Dtos;

public class GetUserLoginDto
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
}
