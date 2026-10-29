using Npgsql;
using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Database;

namespace Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private const string SelectColumns = "Id, SentById, SentInId, Content, SentAt";

    private readonly DatabaseConnection _databaseConnection;

    public MessageRepository(DatabaseConnection databaseConnection)
    {
        _databaseConnection = databaseConnection;
    }

    // Creates a new message in the database.
    public Task CreateMessageAsync(CreateMessageDto message, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using var command = new NpgsqlCommand(
                    "INSERT INTO Messages (Id, SentById, SentInId, Content, SentAt) VALUES (@Id, @SentById, @SentInId, @Content, @SentAt)",
                    connection);
                command.Parameters.AddWithValue("Id", message.Id);
                command.Parameters.AddWithValue("SentById", message.SentById);
                command.Parameters.AddWithValue("SentInId", message.SentInId);
                command.Parameters.AddWithValue("Content", message.Content);
                command.Parameters.AddWithValue("SentAt", message.SentAt);

                await command.ExecuteNonQueryAsync(ct);
            },
            cancellationToken);
    }

    // Retrieves a message by its ID.
    public Task<GetMessageDto?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using var command =
                    new NpgsqlCommand($"SELECT {SelectColumns} FROM Messages WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("Id", messageId);

                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    return ReadMessage(reader);
                }

                return null;
            },
            cancellationToken);
    }

    // Retrieves messages for a specific chat, with optional pagination.
    // Returns the newest messages first. Pass the SentAt of the oldest loaded message as "before" to page further back.
    public Task<List<GetMessageDto>> GetMessagesForChatAsync(
        Guid chatId,
        int limit = 50,
        DateTime? before = null,
        DateTime? after = null,
        CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using var command = new NpgsqlCommand(
                    $@"SELECT {SelectColumns}
                       FROM Messages
                       WHERE SentInId = @ChatId
                         AND (@Before IS NULL OR SentAt < @Before)
                         AND (@After IS NULL OR SentAt > @After)
                       ORDER BY SentAt DESC
                       LIMIT @Limit",
                    connection);
                command.Parameters.AddWithValue("ChatId", chatId);
                command.Parameters.Add(new NpgsqlParameter("Before", NpgsqlTypes.NpgsqlDbType.Timestamp)
                {
                    Value = (object?)before ?? DBNull.Value
                });
                command.Parameters.Add(new NpgsqlParameter("After", NpgsqlTypes.NpgsqlDbType.Timestamp)
                {
                    Value = (object?)after ?? DBNull.Value
                });
                command.Parameters.AddWithValue("Limit", limit);

                var messages = new List<GetMessageDto>();
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    messages.Add(ReadMessage(reader));
                }

                return messages;
            },
            cancellationToken);
    }

    // Deletes a message by its ID.
    public Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return _databaseConnection.WithConnectionAsync(
            async (connection, ct) =>
            {
                await using var command =
                    new NpgsqlCommand("DELETE FROM Messages WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("Id", messageId);

                await command.ExecuteNonQueryAsync(ct);
            },
            cancellationToken);
    }

    // Maps the reader to a GetMessageDto.
    private static GetMessageDto ReadMessage(NpgsqlDataReader reader)
    {
        return new GetMessageDto
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            SentById = reader.GetGuid(reader.GetOrdinal("SentById")),
            SentInId = reader.GetGuid(reader.GetOrdinal("SentInId")),
            Content = reader.GetString(reader.GetOrdinal("Content")),
            SentAt = reader.GetDateTime(reader.GetOrdinal("SentAt"))
        };
    }
}
