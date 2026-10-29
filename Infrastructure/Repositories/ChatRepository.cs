using Domain;
using Npgsql;
using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly DatabaseConnection _databaseConnection;

    public ChatRepository(DatabaseConnection databaseConnection)
    {
        _databaseConnection = databaseConnection;
    }

    // Checks if a chat exists by its ID.
    public Task<bool> ChatExistsAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using var command =
                    new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM Chats WHERE Id = @Id)", connection);
                command.Parameters.AddWithValue("Id", chatId);

                return (bool)(await command.ExecuteScalarAsync(ct))!;
            },
            cancellationToken);
    }

    // Creates a new chat.
    public Task CreateChatAsync(CreateChatDto chat, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                // The chat_type enum values are lowercase in PostgreSQL, so the text is cast explicitly.
                await using var command =
                    new NpgsqlCommand("INSERT INTO Chats (Id, Type, Name, CreatedAt) VALUES (@Id, @Type::chat_type, @Name, @CreatedAt)", connection);
                command.Parameters.AddWithValue("Id", chat.Id);
                command.Parameters.AddWithValue("Type", chat.Type.ToString().ToLowerInvariant());
                command.Parameters.AddWithValue("Name", (object?)chat.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("CreatedAt", chat.CreatedAt);

                await command.ExecuteNonQueryAsync(ct);
            },
            cancellationToken);
    }

    // Retrieves a chat by its ID.
    public Task<GetChatDto?> GetChatAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using var command =
                    new NpgsqlCommand("SELECT Id, Type::text, Name, CreatedAt FROM Chats WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("Id", chatId);

                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    return ReadChat(reader);
                }

                return null;
            },
            cancellationToken);
    }

    // Retrieves all chats for a specific user.
    public Task<List<GetChatDto>> GetChatsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using NpgsqlCommand command = new NpgsqlCommand(
                    @"SELECT
                        Chat.Id,
                        Chat.Type::text,
                        Chat.Name,
                        Chat.CreatedAt
                    FROM Chats AS Chat
                    JOIN ChatMembers AS ChatMember
                        ON ChatMember.ChatId = Chat.Id
                    WHERE ChatMember.UserId = @UserId
                    ORDER BY Chat.CreatedAt DESC",
                    connection);
                command.Parameters.AddWithValue("UserId", userId);

                List<GetChatDto> chats = new List<GetChatDto>();
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    chats.Add(ReadChat(reader));
                }

                return chats;
            },
            cancellationToken);
    }

    // Retrieves a private chat between two users, if it exists.
    public Task<GetChatDto?> GetPrivateChatBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using NpgsqlCommand command = new NpgsqlCommand(
                    @"SELECT
                        Chat.Id,
                        Chat.Type::text,
                        Chat.Name,
                        Chat.CreatedAt
                    FROM Chats AS Chat
                    JOIN ChatMembers AS FirstUserMembership
                        ON FirstUserMembership.ChatId = Chat.Id
                        AND FirstUserMembership.UserId = @FirstUserId
                    JOIN ChatMembers AS SecondUserMembership
                        ON SecondUserMembership.ChatId = Chat.Id
                        AND SecondUserMembership.UserId = @SecondUserId
                    WHERE Chat.Type = 'private'
                    LIMIT 1;",
                    connection);
                command.Parameters.AddWithValue("FirstUserId", firstUserId);
                command.Parameters.AddWithValue("SecondUserId", secondUserId);

                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    GetChatDto chat = ReadChat(reader);
                    return chat;
                }

                return null;
            },
            cancellationToken);
    }

    // Updates the name of a chat.
    public Task UpdateChatNameAsync(Guid chatId, UpdateChatNameDto updateChatNameDto, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using var command =
                    new NpgsqlCommand("UPDATE Chats SET Name = @Name WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("Id", chatId);
                command.Parameters.AddWithValue("Name", (object?)updateChatNameDto.Name ?? DBNull.Value);

                await command.ExecuteNonQueryAsync(ct);
            },
            cancellationToken);
    }

    // Deletes a chat and all its associated messages and members.
    public Task DeleteChatAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                // Messages and members reference the chat, so they must be removed first.
                await using var transaction = await connection.BeginTransactionAsync(ct);

                foreach (string sql in new[]
                {
                    "DELETE FROM Messages WHERE SentInId = @Id",
                    "DELETE FROM ChatMembers WHERE ChatId = @Id",
                    "DELETE FROM Chats WHERE Id = @Id"
                })
                {
                    await using NpgsqlCommand command = new NpgsqlCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("Id", chatId);
                    await command.ExecuteNonQueryAsync(ct);
                }

                await transaction.CommitAsync(ct);
            },
            cancellationToken);
    }

    // Maps a data reader to a GetChatDto.
    private static GetChatDto ReadChat(NpgsqlDataReader reader)
    {
        return new GetChatDto
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            Type = Enum.Parse<EChatType>(reader.GetString(reader.GetOrdinal("Type")), ignoreCase: true),
            Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
