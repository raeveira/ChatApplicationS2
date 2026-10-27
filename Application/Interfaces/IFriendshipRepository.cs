using Application.Dtos;

namespace Application.Interfaces;

public interface IFriendshipRepository
{
    Task CreateFriendshipAsync(CreateFriendshipDto friendship, CancellationToken cancellationToken = default);
    // Finds the friendship between two users, whoever sent the request.
    Task<GetFriendshipDto?> GetFriendshipBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default);
    Task AcceptFriendshipAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);
    Task DeleteFriendshipBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default);
    Task<List<GetFriendDto>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<GetFriendRequestDto>> GetPendingRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
}
