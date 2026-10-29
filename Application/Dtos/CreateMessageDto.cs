namespace Application.Dtos;

public class CreateMessageDto
{
    public Guid Id { get; set; }
    public Guid SentById { get; set; }
    public Guid SentInId { get; set; }
    public required string Content { get; set; }
    public DateTime SentAt { get; set; }
}
