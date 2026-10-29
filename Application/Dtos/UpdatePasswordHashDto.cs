namespace Application.Dtos;

public class UpdatePasswordHashDto
{
    public required string PasswordHash { get; set; }
    public DateTime UpdatedAt { get; set; }
}
