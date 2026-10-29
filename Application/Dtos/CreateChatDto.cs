using Domain;

namespace Application.Dtos;

public class CreateChatDto
{
    public Guid Id { get; set; }
    public EChatType Type { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
