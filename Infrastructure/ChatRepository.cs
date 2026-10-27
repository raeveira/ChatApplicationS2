using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Npgsql;
using Application.Dtos;
using Application.Interfaces;

namespace Infrastructure
{
    public class ChatRepository : IChatRepository
    {
        private readonly DatabaseConnection _databaseConnection;

        public ChatRepository(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

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

        public Task<List<GetChatDto>> GetChatsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        @"SELECT c.Id, c.Type::text, c.Name, c.CreatedAt
                          FROM Chats c
                          JOIN ChatMembers cm ON cm.ChatId = c.Id
                          WHERE cm.UserId = @UserId
                          ORDER BY c.CreatedAt DESC",
                        connection);
                    command.Parameters.AddWithValue("UserId", userId);

                    var chats = new List<GetChatDto>();
                    await using var reader = await command.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        chats.Add(ReadChat(reader));
                    }

                    return chats;
                },
                cancellationToken);
        }

        public Task<GetChatDto?> GetPrivateChatBetweenAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        @"SELECT c.Id, c.Type::text, c.Name, c.CreatedAt
                          FROM Chats c
                          JOIN ChatMembers memberA ON memberA.ChatId = c.Id AND memberA.UserId = @FirstUserId
                          JOIN ChatMembers memberB ON memberB.ChatId = c.Id AND memberB.UserId = @SecondUserId
                          WHERE c.Type = 'private'
                          LIMIT 1",
                        connection);
                    command.Parameters.AddWithValue("FirstUserId", firstUserId);
                    command.Parameters.AddWithValue("SecondUserId", secondUserId);

                    await using var reader = await command.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        return ReadChat(reader);
                    }

                    return null;
                },
                cancellationToken);
        }

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

        public Task DeleteChatAsync(Guid chatId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    // Messages and members reference the chat, so they must be removed first.
                    await using var transaction = await connection.BeginTransactionAsync(ct);

                    foreach (var sql in new[]
                    {
                        "DELETE FROM Messages WHERE SentInId = @Id",
                        "DELETE FROM ChatMembers WHERE ChatId = @Id",
                        "DELETE FROM Chats WHERE Id = @Id"
                    })
                    {
                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        command.Parameters.AddWithValue("Id", chatId);
                        await command.ExecuteNonQueryAsync(ct);
                    }

                    await transaction.CommitAsync(ct);
                },
                cancellationToken);
        }

        private static GetChatDto ReadChat(NpgsqlDataReader reader)
        {
            return new GetChatDto
            {
                Id = reader.GetGuid(0),
                Type = Enum.Parse<EChatType>(reader.GetString(1), ignoreCase: true),
                Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }
    }
}