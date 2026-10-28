using Npgsql;

namespace Infrastructure.Database;

public class DatabaseInitializer(NpgsqlDataSource dataSource)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        NpgsqlCommand command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);

        // Create Enum Types
        command.CommandText = @"
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

            DO $$ BEGIN
                CREATE TYPE friendship_status AS ENUM ('pending', 'accepted');
            EXCEPTION
                WHEN duplicate_object THEN null;
            END $$;
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Users table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id UUID PRIMARY KEY,
                Username TEXT UNIQUE NOT NULL,
                DisplayName TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                IconUrl TEXT,
                CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
                UpdatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
                AllowDirectMessagesFromEveryone BOOLEAN NOT NULL DEFAULT TRUE,

                CONSTRAINT chk_username CHECK (Username = LOWER(Username)),
                CONSTRAINT chk_displayname CHECK (DisplayName = TRIM(DisplayName))
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Chats table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Chats (
                Id UUID PRIMARY KEY,
                type chat_type NOT NULL,
                Name TEXT,
                CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
                CHECK (type = 'group' OR Name IS NULL)
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // ChatMembers table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ChatMembers (
                ChatId UUID NOT NULL REFERENCES Chats(Id) ON DELETE CASCADE,
                UserId UUID NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                Role chat_member_role NOT NULL,
                JoinedAt TIMESTAMP NOT NULL DEFAULT NOW(),
                PRIMARY KEY (ChatId, UserId)
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Messages table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Messages (
                Id UUID PRIMARY KEY,
                SentInId UUID NOT NULL REFERENCES Chats(Id) ON DELETE CASCADE,
                SentById UUID NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                Content TEXT NOT NULL,
                SentAt TIMESTAMP NOT NULL DEFAULT NOW()
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Friendships table
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Friendships (
                RequesterId UUID NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                AddresseeId UUID NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                Status friendship_status NOT NULL,
                CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
                PRIMARY KEY (RequesterId, AddresseeId),
                CHECK (RequesterId <> AddresseeId)
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
