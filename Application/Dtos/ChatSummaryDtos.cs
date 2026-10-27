using Domain;

namespace Application.Dtos;

// What the sidebar and chat header show: a private chat shows the other user, a group shows its name.
public class GetChatSummaryDto
{
    public Guid ChatId { get; set; }
    public EChatType Type { get; set; }
    public required string Title { get; set; }
    public required string Subtitle { get; set; }
    public string? IconUrl { get; set; }
    public int MemberCount { get; set; }
}

public class GetChatMemberProfileDto
{
    public Guid UserId { get; set; }
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
    public string? IconUrl { get; set; }
    public EChatMemberRole Role { get; set; }
}
