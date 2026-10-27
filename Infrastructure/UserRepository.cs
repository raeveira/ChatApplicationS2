using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Application.Dtos;
using Application.Interfaces;

namespace Infrastructure
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseConnection _databaseConnection;

        public UserRepository(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM Users WHERE Id = @Id)", connection);
                    command.Parameters.AddWithValue("Id", userId);

                    return (bool)await command.ExecuteScalarAsync(ct);
                },
                cancellationToken);
        }

        public Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("DELETE FROM Users WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("Id", userId);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        public Task UpdateUsernameAsync(Guid userId, UpdateUsernameDto updateUsernameDto, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("UPDATE Users SET Username = @Username, UpdatedAt = @UpdatedAt WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("Id", userId);
                    command.Parameters.AddWithValue("Username", updateUsernameDto.Username);
                    command.Parameters.AddWithValue("UpdatedAt", updateUsernameDto.UpdatedAt);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        public Task UpdatePasswordHashAsync(Guid userId, UpdatePasswordHashDto updatePasswordHashDto, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("UPDATE Users SET PasswordHash = @PasswordHash, UpdatedAt = @UpdatedAt WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("Id", userId);
                    command.Parameters.AddWithValue("PasswordHash", updatePasswordHashDto.PasswordHash);
                    command.Parameters.AddWithValue("UpdatedAt", updatePasswordHashDto.UpdatedAt);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        public Task<GetUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                // using ct for cancellation token in the async lambda
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("SELECT Id, Username, DisplayName, IconUrl, CreatedAt, UpdatedAt, AllowDirectMessagesFromEveryone FROM Users WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("Id", userId);

                    await using var reader = await command.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        return new GetUserDto
                        {
                            Id = reader.GetGuid(0),
                            Username = reader.GetString(1),
                            DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                            IconUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                            CreatedAt = reader.GetDateTime(4),
                            UpdatedAt = reader.GetDateTime(5),
                            AllowDirectMessagesFromEveryone = reader.GetBoolean(6)
                        };
                    }

                    return null;
                },
                cancellationToken);
        }

        public Task<GetUserDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("SELECT Id, Username, DisplayName, IconUrl, CreatedAt, UpdatedAt, AllowDirectMessagesFromEveryone FROM Users WHERE Username = @Username", connection);
                    command.Parameters.AddWithValue("Username", username);

                    await using var reader = await command.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        return new GetUserDto
                        {
                            Id = reader.GetGuid(0),
                            Username = reader.GetString(1),
                            DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                            IconUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                            CreatedAt = reader.GetDateTime(4),
                            UpdatedAt = reader.GetDateTime(5),
                            AllowDirectMessagesFromEveryone = reader.GetBoolean(6)
                        };
                    }

                    return null;
                },
                cancellationToken);
        }

        public Task<GetUserLoginDto?> GetUserLoginAsync(string username, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("SELECT Id, Username, PasswordHash FROM Users WHERE Username = @Username", connection);
                    command.Parameters.AddWithValue("Username", username);

                    await using var reader = await command.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        return new GetUserLoginDto
                        {
                            Id = reader.GetGuid(0),
                            Username = reader.GetString(1),
                            PasswordHash = reader.GetString(2)
                        };
                    }

                    return null;
                },
                cancellationToken);
        }

        public Task CreateUserAsync(CreateUserDto user, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("INSERT INTO Users (Id, Username, DisplayName, PasswordHash, IconUrl, CreatedAt, UpdatedAt) VALUES (@Id, @Username, @DisplayName, @PasswordHash, @IconUrl, @CreatedAt, @UpdatedAt)", connection);
                    command.Parameters.AddWithValue("Id", user.Id);
                    command.Parameters.AddWithValue("Username", user.Username);
                    command.Parameters.AddWithValue("DisplayName", (object?)user.DisplayName ?? DBNull.Value);
                    command.Parameters.AddWithValue("PasswordHash", user.PasswordHash);
                    command.Parameters.AddWithValue("IconUrl", (object?)user.IconUrl ?? DBNull.Value);
                    command.Parameters.AddWithValue("CreatedAt", user.CreatedAt);
                    command.Parameters.AddWithValue("UpdatedAt", user.UpdatedAt);
                    
                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        public Task UpdateAllowDirectMessagesFromEveryoneAsync(Guid userId, bool allowDirectMessagesFromEveryone, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("UPDATE Users SET AllowDirectMessagesFromEveryone = @Allow WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("Allow", allowDirectMessagesFromEveryone);
                    command.Parameters.AddWithValue("Id", userId);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

        public Task UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default)
        {
            return _databaseConnection.WithConnectionAsync(
                async (connection, ct) =>
                {
                    await using var command =
                        new NpgsqlCommand("UPDATE Users SET DisplayName = @DisplayName, IconUrl = @IconUrl, UpdatedAt = @UpdatedAt WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("DisplayName", (object?)updateUserDto.DisplayName ?? DBNull.Value);
                    command.Parameters.AddWithValue("IconUrl", (object?)updateUserDto.IconUrl ?? DBNull.Value);
                    command.Parameters.AddWithValue("UpdatedAt", updateUserDto.UpdatedAt);
                    command.Parameters.AddWithValue("Id", userId);

                    await command.ExecuteNonQueryAsync(ct);
                },
                cancellationToken);
        }

    }
}