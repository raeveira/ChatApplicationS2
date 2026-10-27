using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class FriendService(IFriendshipRepository friendshipRepository, IUserRepository userRepository)
{
    public async Task SendFriendRequestAsync(
        Guid currentUserId,
        string targetUsername,
        CancellationToken cancellationToken = default)
    {
        var targetUser = await userRepository.GetUserByUsernameAsync(
                             targetUsername.Trim().ToLowerInvariant(), cancellationToken)
                         ?? throw new UserNotFoundException();

        // The domain rejects a request to yourself.
        var request = Friendship.CreateRequest(currentUserId, targetUser.Id);

        var existing = await friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUser.Id, cancellationToken);
        if (existing is not null)
        {
            throw existing.Status == EFriendshipStatus.Accepted
                ? new AlreadyFriendsException()
                : new FriendRequestAlreadyExistsException();
        }

        await friendshipRepository.CreateFriendshipAsync(new CreateFriendshipDto
        {
            RequesterId = request.RequesterId,
            AddresseeId = request.AddresseeId,
            Status = request.Status,
            CreatedAt = request.CreatedAt
        }, cancellationToken);
    }

    public async Task AcceptFriendRequestAsync(
        Guid currentUserId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var stored = await friendshipRepository.GetFriendshipBetweenAsync(currentUserId, requesterUserId, cancellationToken);

        // Only the user who received the request can accept it.
        if (stored is null || stored.RequesterId != requesterUserId || stored.AddresseeId != currentUserId)
            throw new FriendRequestNotFoundException();

        var friendship = Friendship.Restore(stored.RequesterId, stored.AddresseeId, stored.Status, stored.CreatedAt);
        friendship.Accept();

        await friendshipRepository.AcceptFriendshipAsync(requesterUserId, currentUserId, cancellationToken);
    }

    // Used for declining an incoming request and for cancelling an outgoing one.
    public async Task RemoveFriendRequestAsync(
        Guid currentUserId,
        Guid otherUserId,
        CancellationToken cancellationToken = default)
    {
        var stored = await friendshipRepository.GetFriendshipBetweenAsync(currentUserId, otherUserId, cancellationToken);
        if (stored is null || stored.Status != EFriendshipStatus.Pending)
            throw new FriendRequestNotFoundException();

        await friendshipRepository.DeleteFriendshipBetweenAsync(currentUserId, otherUserId, cancellationToken);
    }

    public async Task RemoveFriendAsync(
        Guid currentUserId,
        Guid friendUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await AreFriendsAsync(currentUserId, friendUserId, cancellationToken))
            throw new NotFriendsException();

        await friendshipRepository.DeleteFriendshipBetweenAsync(currentUserId, friendUserId, cancellationToken);
    }

    public Task<List<GetFriendDto>> GetFriendsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        return friendshipRepository.GetFriendsAsync(currentUserId, cancellationToken);
    }

    public Task<List<GetFriendRequestDto>> GetPendingRequestsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        return friendshipRepository.GetPendingRequestsAsync(currentUserId, cancellationToken);
    }

    private async Task<bool> AreFriendsAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken)
    {
        var friendship = await friendshipRepository.GetFriendshipBetweenAsync(firstUserId, secondUserId, cancellationToken);
        return friendship?.Status == EFriendshipStatus.Accepted;
    }
}
