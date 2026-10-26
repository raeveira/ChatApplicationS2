using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<DatabaseConnection>(provider => new DatabaseConnection(connectionString));
        
        services.AddScoped<UserRepository>();
        services.AddScoped<ChatRepository>();
        services.AddScoped<ChatMemberRepository>();
        services.AddScoped<MessageRepository>();
        
        return services;
    }
}
