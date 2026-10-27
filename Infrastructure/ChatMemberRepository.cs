using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Npgsql;
using Application.Dtos;
using Application.Interfaces;

namespace Infrastructure
{
    public class ChatMemberRepository : IChatMemberRepository
    {
        private readonly DatabaseConnection _databaseConnection;

        public ChatMemberRepository(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        public Task AddMemberAsync(AddChatMemberDto member, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    // The chat_member_role enum values are lowercase in PostgreSQL, so the text is cast explicitly.
                    await using var command = new NpgsqlCommand(
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

        public Task RemoveMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        "DELETE FROM ChatMembers WHERE ChatId = @ChatId AND UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("ChatId", chatId);
                    command.Parameters.AddWithValue("UserId", userId);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        public Task<bool> IsMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
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
                    await using var command = new NpgsqlCommand(
                        "SELECT ChatId, UserId, Role::text, JoinedAt FROM ChatMembers WHERE ChatId = @ChatId AND UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("ChatId", chatId);
                    command.Parameters.AddWithValue("UserId", userId);

                    await using var reader = await command.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        return ReadMember(reader);
                    }

                    return null;
                },
                cancellationToken);
        }

        public Task<List<GetChatMemberDto>> GetMembersAsync(Guid chatId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        "SELECT ChatId, UserId, Role::text, JoinedAt FROM ChatMembers WHERE ChatId = @ChatId ORDER BY JoinedAt",
                        connection);
                    command.Parameters.AddWithValue("ChatId", chatId);

                    var members = new List<GetChatMemberDto>();
                    await using var reader = await command.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        members.Add(ReadMember(reader));
                    }

                    return members;
                },
                cancellationToken);
        }

        public Task UpdateMemberRoleAsync(Guid chatId, Guid userId, EChatMemberRole role, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command = new NpgsqlCommand(
                        "UPDATE ChatMembers SET Role = @Role::chat_member_role WHERE ChatId = @ChatId AND UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("ChatId", chatId);
                    command.Parameters.AddWithValue("UserId", userId);
                    command.Parameters.AddWithValue("Role", role.ToString().ToLowerInvariant());

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        private static GetChatMemberDto ReadMember(NpgsqlDataReader reader)
        {
            return new GetChatMemberDto
            {
                ChatId = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                Role = Enum.Parse<EChatMemberRole>(reader.GetString(2), ignoreCase: true),
                JoinedAt = reader.GetDateTime(3)
            };
        }
    }
}
