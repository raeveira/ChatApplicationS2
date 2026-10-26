using Domain;

namespace Infrastructure.Dtos
{
    public class GetChatDto
    {
        public Guid Id { get; set; }
        public EChatType Type { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
