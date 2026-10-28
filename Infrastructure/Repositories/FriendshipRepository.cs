using Application.Dtos;
using Application.Interfaces;
using Domain;
using Npgsql;
using Infrastructure.Database;

namespace Infrastructure.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly DatabaseConnection _databaseConnection;

        public FriendshipRepository(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        // Creates a new friendship record in the database.
        public Task CreateFriendshipAsync(CreateFriendshipDto friendship, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        "INSERT INTO Friendships (RequesterId, AddresseeId, Status, CreatedAt) VALUES (@RequesterId, @AddresseeId, @Status::friendship_status, @CreatedAt)",
                        connection);
                    command.Parameters.AddWithValue("RequesterId", friendship.RequesterId);
                    command.Parameters.AddWithValue("AddresseeId", friendship.AddresseeId);
                    command.Parameters.AddWithValue("Status", friendship.Status.ToString().ToLowerInvariant());
                    command.Parameters.AddWithValue("CreatedAt", friendship.CreatedAt);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        // Retrieves a friendship record between two users, if it exists.
        public Task<GetFriendshipDto?> GetFriendshipBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        @"SELECT RequesterId, AddresseeId, Status::text, CreatedAt
                          FROM Friendships
                          WHERE (RequesterId = @FirstUserId AND AddresseeId = @SecondUserId)
                             OR (RequesterId = @SecondUserId AND AddresseeId = @FirstUserId)",
                        connection);
                    command.Parameters.AddWithValue("FirstUserId", firstUserId);
                    command.Parameters.AddWithValue("SecondUserId", secondUserId);

                    await using var reader = await command.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        return new GetFriendshipDto
                        {
                            RequesterId = reader.GetGuid(0),
                            AddresseeId = reader.GetGuid(1),
                            Status = Enum.Parse<EFriendshipStatus>(reader.GetString(2), ignoreCase: true),
                            CreatedAt = reader.GetDateTime(3)
                        };
                    }

                    return null;
                },
                cancellationToken);
        }

        // Accepts a pending friendship request.
        public Task AcceptFriendshipAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        "UPDATE Friendships SET Status = 'accepted' WHERE RequesterId = @RequesterId AND AddresseeId = @AddresseeId",
                        connection);
                    command.Parameters.AddWithValue("RequesterId", requesterId);
                    command.Parameters.AddWithValue("AddresseeId", addresseeId);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        // Deletes a friendship record between two users, if it exists.
        public Task DeleteFriendshipBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        @"DELETE FROM Friendships
                          WHERE (RequesterId = @FirstUserId AND AddresseeId = @SecondUserId)
                             OR (RequesterId = @SecondUserId AND AddresseeId = @FirstUserId)",
                        connection);
                    command.Parameters.AddWithValue("FirstUserId", firstUserId);
                    command.Parameters.AddWithValue("SecondUserId", secondUserId);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        // Retrieves all friends of a specific user.
        public Task<List<GetFriendDto>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        @"SELECT
                            Friend.Id,
                            Friend.Username,
                            Friend.DisplayName,
                            Friend.IconUrl
                        FROM Friendships AS Friendship
                        JOIN Users AS Friend
                            ON Friend.Id = CASE
                                WHEN Friendship.RequesterId = @UserId
                                    THEN Friendship.AddresseeId
                                ELSE Friendship.RequesterId
                            END
                        WHERE Friendship.Status = 'accepted'
                            AND (
                                Friendship.RequesterId = @UserId
                                OR Friendship.AddresseeId = @UserId
                            )
                        ORDER BY Friend.DisplayName",
                    connection);
                    command.Parameters.AddWithValue("UserId", userId);

                    var friends = new List<GetFriendDto>();
                    await using var reader = await command.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        friends.Add(new GetFriendDto
                        {
                            UserId = reader.GetGuid(0),
                            Username = reader.GetString(1),
                            DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                            IconUrl = reader.IsDBNull(3) ? null : reader.GetString(3)
                        });
                    }

                    return friends;
                },
                cancellationToken);
        }

        // Get all the pending friend requests for a user
        public Task<List<GetFriendRequestDto>> GetPendingRequestsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        @"SELECT
                            Friend.Id,
                            Friend.Username,
                            Friend.DisplayName,
                            Friend.IconUrl,
                            (Friendship.AddresseeId = @UserId) AS IsIncoming
                        FROM Friendships AS Friendship
                        JOIN Users AS Friend
                            ON Friend.Id = CASE
                                WHEN Friendship.RequesterId = @UserId
                                    THEN Friendship.AddresseeId
                                ELSE Friendship.RequesterId
                            END
                        WHERE Friendship.Status = 'pending'
                            AND (
                                Friendship.RequesterId = @UserId
                                OR Friendship.AddresseeId = @UserId
                            )
                        ORDER BY Friendship.CreatedAt DESC",
                    connection);
                    command.Parameters.AddWithValue("UserId", userId);

                    var requests = new List<GetFriendRequestDto>();
                    await using var reader = await command.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        requests.Add(new GetFriendRequestDto
                        {
                            UserId = reader.GetGuid(0),
                            Username = reader.GetString(1),
                            DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                            IconUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsIncoming = reader.GetBoolean(4)
                        });
                    }

                    return requests;
                },
                cancellationToken);
        }
    }
}
