namespace Application.Dtos;

public class UpdateUserDto
{
    public string? DisplayName { get; set; }
    public string? IconUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
}
