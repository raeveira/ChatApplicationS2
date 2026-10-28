using Application.Interfaces;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure;

public static class DependencyInjection
{
    // Extension method to add infrastructure services to the DI container
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<DatabaseConnection>(provider => new DatabaseConnection(connectionString));
        
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IChatMemberRepository, ChatMemberRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        
        return services;
    }
}
