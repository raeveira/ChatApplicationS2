using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Infrastructure.Database
{
    public class DatabaseConnection
    {
        private readonly string _connectionString;

        public DatabaseConnection(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Retrieves a new database connection.
        private async Task<NpgsqlConnection> GetConnection()
        {
            NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        // Executes a function with a database connection and returns a result.
        public async Task WithConnectionAsync(Func<NpgsqlConnection, CancellationToken, Task> func, CancellationToken cancellationToken = default)
        {
            NpgsqlConnection connection = await GetConnection();
            await func(connection, cancellationToken);
        }

        // Executes a function with a database connection and returns a result of type T.
        public async Task<T> WithConnectionAsync<T>(Func<NpgsqlConnection, CancellationToken, Task<T>> func, CancellationToken cancellationToken = default)
        {
            NpgsqlConnection connection = await GetConnection();
            return await func(connection, cancellationToken);
        }
    }
}