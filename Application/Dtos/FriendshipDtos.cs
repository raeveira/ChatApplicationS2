using Domain;

namespace Application.Dtos;

public class CreateFriendshipDto
{
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public EFriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetFriendshipDto
{
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public EFriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

// An accepted friend, with the profile details needed to show them in a list.
public class GetFriendDto
{
    public Guid UserId { get; set; }
    public required string Username { get; set; }
    public string? DisplayName { get; set; }
    public string? IconUrl { get; set; }
}

// A pending request. IsIncoming is true when the other user sent it to you.
public class GetFriendRequestDto
{
    public Guid UserId { get; set; }
    public required string Username { get; set; }
    public string? DisplayName { get; set; }
    public string? IconUrl { get; set; }
    public bool IsIncoming { get; set; }
}
