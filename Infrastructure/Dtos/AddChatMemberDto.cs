using Domain;

namespace Infrastructure.Dtos
{
    public class AddChatMemberDto
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public EChatMemberRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
