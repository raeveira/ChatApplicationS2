using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
    public async Task<GetUserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await userRepository.GetUserAsync(userId, cancellationToken)
            ?? throw new UserNotFoundException();
    }

    public async Task ChangeDisplayNameAsync(Guid userId, string newDisplayName, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var displayName = User.ValidateDisplayName(newDisplayName);

        await userRepository.UpdateUserAsync(userId, new UpdateUserDto
        {
            DisplayName = displayName,
            IconUrl = user.IconUrl!,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task ChangeUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default)
    {
        var username = User.ValidateUsername(newUsername);

        var existingUser = await userRepository.GetUserByUsernameAsync(username, cancellationToken);
        if (existingUser is not null && existingUser.Id != userId)
            throw new UsernameTakenException(username);

        await userRepository.UpdateUsernameAsync(userId, new UpdateUsernameDto
        {
            Username = username,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        AuthService.ValidatePassword(newPassword);
        await EnsurePasswordIsCorrectAsync(userId, currentPassword, cancellationToken);

        await userRepository.UpdatePasswordHashAsync(userId, new UpdatePasswordHashDto
        {
            PasswordHash = passwordHasher.Hash(newPassword),
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public Task SetAllowDirectMessagesFromEveryoneAsync(
        Guid userId,
        bool allowDirectMessagesFromEveryone,
        CancellationToken cancellationToken = default)
    {
        return userRepository.UpdateAllowDirectMessagesFromEveryoneAsync(userId, allowDirectMessagesFromEveryone, cancellationToken);
    }

    public async Task DeleteAccountAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        await EnsurePasswordIsCorrectAsync(userId, password, cancellationToken);
        await userRepository.DeleteUserAsync(userId, cancellationToken);
    }

    private async Task EnsurePasswordIsCorrectAsync(Guid userId, string password, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var login = await userRepository.GetUserLoginAsync(user.Username, cancellationToken);

        if (login is null || string.IsNullOrEmpty(password) || !passwordHasher.Verify(password, login.PasswordHash))
            throw new IncorrectPasswordException();
    }
}
