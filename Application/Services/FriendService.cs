using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class FriendService(IFriendshipRepository friendshipRepository, IUserRepository userRepository)
{
    // Sends a friend request from the current user to the target user.
    public async Task SendFriendRequestAsync(
        Guid currentUserId,
        string targetUsername,
        CancellationToken cancellationToken = default)
    {
        GetUserDto targetUser = await userRepository.GetUserByUsernameAsync(
                             targetUsername.Trim().ToLowerInvariant(), cancellationToken)
                         ?? throw new UserNotFoundException();

        // The domain rejects a request to yourself.
        Friendship request = Friendship.CreateRequest(currentUserId, targetUser.Id);

        GetFriendshipDto? existing = await friendshipRepository.GetFriendshipBetweenAsync(currentUserId, targetUser.Id, cancellationToken);
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

    // Accepts an incoming friend request from the specified user.
    public async Task AcceptFriendRequestAsync(
        Guid currentUserId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
    {
        GetFriendshipDto? stored = await friendshipRepository.GetFriendshipBetweenAsync(currentUserId, requesterUserId, cancellationToken);

        // Only the user who received the request can accept it.
        if (stored is null || stored.RequesterId != requesterUserId || stored.AddresseeId != currentUserId)
            throw new FriendRequestNotFoundException();

        Friendship friendship = Friendship.Restore(stored.RequesterId, stored.AddresseeId, stored.Status, stored.CreatedAt);
        friendship.Accept();

        await friendshipRepository.AcceptFriendshipAsync(requesterUserId, currentUserId, cancellationToken);
    }

    // Used for declining an incoming request and for cancelling an outgoing one.
    public async Task RemoveFriendRequestAsync(
        Guid currentUserId,
        Guid otherUserId,
        CancellationToken cancellationToken = default)
    {
        GetFriendshipDto? stored = await friendshipRepository.GetFriendshipBetweenAsync(currentUserId, otherUserId, cancellationToken);
        if (stored is null || stored.Status != EFriendshipStatus.Pending)
            throw new FriendRequestNotFoundException();

        await friendshipRepository.DeleteFriendshipBetweenAsync(currentUserId, otherUserId, cancellationToken);
    }

    // Removes an existing friend relationship.
    public async Task RemoveFriendAsync(
        Guid currentUserId,
        Guid friendUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await AreFriendsAsync(currentUserId, friendUserId, cancellationToken))
            throw new NotFriendsException();

        await friendshipRepository.DeleteFriendshipBetweenAsync(currentUserId, friendUserId, cancellationToken);
    }

    // Retrieves the list of friends for the current user.
    public Task<List<GetFriendDto>> GetFriendsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        return friendshipRepository.GetFriendsAsync(currentUserId, cancellationToken);
    }

    // Retrieves the list of pending friend requests for the current user.
    public Task<List<GetFriendRequestDto>> GetPendingRequestsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        return friendshipRepository.GetPendingRequestsAsync(currentUserId, cancellationToken);
    }

    // Checks if two users are friends.
    private async Task<bool> AreFriendsAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken)
    {
        GetFriendshipDto? friendship = await friendshipRepository.GetFriendshipBetweenAsync(firstUserId, secondUserId, cancellationToken);
        return friendship?.Status == EFriendshipStatus.Accepted;
    }
}
