using Domain;
using Npgsql;
using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public class ChatMemberRepository : IChatMemberRepository
{
    private readonly DatabaseConnection _databaseConnection;

    public ChatMemberRepository(DatabaseConnection databaseConnection)
    {
        _databaseConnection = databaseConnection;
    }

    // Adds a member to a chat.
    public Task AddMemberAsync(AddChatMemberDto member, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                // The chat_member_role enum values are lowercase in PostgreSQL, so the text is cast explicitly.
                NpgsqlCommand command = new NpgsqlCommand(
                    "INSERT INTO ChatMembers (ChatId, UserId, Role, JoinedAt) VALUES (@ChatId, @UserId, @Role::chat_member_role, @JoinedAt)",
                    connection);
                command.Parameters.AddWithValue("ChatId", member.ChatId);
                command.Parameters.AddWithValue("UserId", member.UserId);
                command.Parameters.AddWithValue("Role", member.Role.ToString().ToLowerInvariant());
                command.Parameters.AddWithValue("JoinedAt", member.JoinedAt);

                await command.ExecuteNonQueryAsync(ct);
            },
            cancellationToken);
    }

    // Removes a member from a chat.
    public Task RemoveMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                NpgsqlCommand command = new NpgsqlCommand(
                    "DELETE FROM ChatMembers WHERE ChatId = @ChatId AND UserId = @UserId",
                    connection);
                command.Parameters.AddWithValue("ChatId", chatId);
                command.Parameters.AddWithValue("UserId", userId);

                await command.ExecuteNonQueryAsync(ct);
            },
            cancellationToken);
    }

    // Checks if a user is a member of a chat.
    public Task<bool> IsMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                NpgsqlCommand command = new NpgsqlCommand(
                    "SELECT EXISTS(SELECT 1 FROM ChatMembers WHERE ChatId = @ChatId AND UserId = @UserId)",
                    connection);
                command.Parameters.AddWithValue("ChatId", chatId);
                command.Parameters.AddWithValue("UserId", userId);

                return (bool)(await command.ExecuteScalarAsync(ct))!;
            },
            cancellationToken);
    }

    public Task<GetChatMemberDto?> GetMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                NpgsqlCommand command = new NpgsqlCommand(
                    "SELECT ChatId, UserId, Role::text, JoinedAt FROM ChatMembers WHERE ChatId = @ChatId AND UserId = @UserId",
                    connection);
                command.Parameters.AddWithValue("ChatId", chatId);
                command.Parameters.AddWithValue("UserId", userId);

                NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    return ReadMember(reader);
                }

                return null;
            },
            cancellationToken);
    }

    // Retrieves all members of a chat.
    public Task<List<GetChatMemberDto>> GetMembersAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                NpgsqlCommand command = new NpgsqlCommand(
                    "SELECT ChatId, UserId, Role::text, JoinedAt FROM ChatMembers WHERE ChatId = @ChatId ORDER BY JoinedAt",
                    connection);
                command.Parameters.AddWithValue("ChatId", chatId);

                var members = new List<GetChatMemberDto>();
                NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    members.Add(ReadMember(reader));
                }

                return members;
            },
            cancellationToken);
    }

    // Updates the role of a chat member.
    public Task UpdateMemberRoleAsync(Guid chatId, Guid userId, EChatMemberRole role, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                NpgsqlCommand command = new NpgsqlCommand(
                    "UPDATE ChatMembers SET Role = @Role::chat_member_role WHERE ChatId = @ChatId AND UserId = @UserId",
                    connection);
                command.Parameters.AddWithValue("ChatId", chatId);
                command.Parameters.AddWithValue("UserId", userId);
                command.Parameters.AddWithValue("Role", role.ToString().ToLowerInvariant());

                await command.ExecuteNonQueryAsync(ct);
            },
            cancellationToken);
    }

    // Maps a data reader to a GetChatMemberDto.
    private static GetChatMemberDto ReadMember(NpgsqlDataReader reader)
    {
        return new GetChatMemberDto
        {
            ChatId = reader.GetGuid(reader.GetOrdinal("ChatId")),
            UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
            Role = Enum.Parse<EChatMemberRole>(reader.GetString(reader.GetOrdinal("Role")), ignoreCase: true),
            JoinedAt = reader.GetDateTime(reader.GetOrdinal("JoinedAt"))
        };
    }
}
