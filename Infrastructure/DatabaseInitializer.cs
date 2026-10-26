using Npgsql;

namespace Infrastructure;

public class DatabaseInitializer(NpgsqlDataSource dataSource)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);

        // Table/column creation scripts go here.

        // Create Enum Types
        await command.CommandText = @"
            DO $$ BEGIN
                CREATE TYPE chat_type AS ENUM ('private', 'group');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;

            DO $$ BEGIN
                CREATE TYPE chat_member_role AS ENUM ('member', 'admin');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Users table
        await command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id SERIAL PRIMARY KEY,
                Username TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                IconUrl TEXT,
                CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
                UpdatedAt TIMESTAMP NOT NULL DEFAULT NOW()
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Chats table
        await command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Chats (
                Id SERIAL PRIMARY KEY,
                type chat_type NOT NULL,
                Name TEXT,
                CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
                CHECK (type = 'group' OR Name IS NULL)
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // ChatMembers table
        await command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ChatMembers (
                ChatId INT NOT NULL REFERENCES Chats(Id),
                UserId INT NOT NULL REFERENCES Users(Id),
                Role chat_member_role NOT NULL,
                JoinedAt TIMESTAMP NOT NULL DEFAULT NOW(),
                PRIMARY KEY (ChatId, UserId)
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Messages table
        await command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Messages (
                Id SERIAL PRIMARY KEY,
                SendInId INT NOT NULL REFERENCES Chats(Id),
                SentById INT NOT NULL REFERENCES Users(Id),
                Content TEXT NOT NULL,
                SentAt TIMESTAMP NOT NULL DEFAULT NOW()
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
