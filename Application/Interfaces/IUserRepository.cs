using Application.Dtos;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateUsernameAsync(Guid userId, UpdateUsernameDto updateUsernameDto, CancellationToken cancellationToken = default);
    Task UpdatePasswordHashAsync(Guid userId, UpdatePasswordHashDto updatePasswordHashDto, CancellationToken cancellationToken = default);
    Task<GetUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<GetUserDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<GetUserLoginDto?> GetUserLoginAsync(string username, CancellationToken cancellationToken = default);
    Task CreateUserAsync(CreateUserDto user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default);
}
