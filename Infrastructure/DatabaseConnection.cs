using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Infrastructure
{
    public class DatabaseConnection
    {
        private readonly string _connectionString;

        public DatabaseConnection(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task<NpgsqlConnection> GetConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task WithConnectionAsync(Func<NpgsqlConnection, CancellationToken, Task> func, CancellationToken cancellationToken = default)
        {
            using var connection = await GetConnection();
            await func(connection, cancellationToken);
        }

        public async Task<T> WithConnectionAsync<T>(Func<NpgsqlConnection, CancellationToken, Task<T>> func, CancellationToken cancellationToken = default)
        {
            using var connection = await GetConnection();
            return await func(connection, cancellationToken);
        }
        
    }
}