using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
    // Retrieves the user with the specified ID.
    public async Task<GetUserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await userRepository.GetUserAsync(userId, cancellationToken)
            ?? throw new UserNotFoundException();
    }

    // Changes the display name of the specified user.
    public async Task ChangeDisplayNameAsync(Guid userId, string newDisplayName, CancellationToken cancellationToken = default)
    {
        GetUserDto user = await GetUserAsync(userId, cancellationToken);
        string displayName = User.ValidateDisplayName(newDisplayName);

        await userRepository.UpdateUserAsync(userId, new UpdateUserDto
        {
            DisplayName = displayName,
            IconUrl = user.IconUrl!,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    // Changes the username of the specified user. Throws if the username is taken.
    public async Task ChangeUsernameAsync(Guid userId, string newUsername, CancellationToken cancellationToken = default)
    {
        string username = User.ValidateUsername(newUsername);

        GetUserDto? existingUser = await userRepository.GetUserByUsernameAsync(username, cancellationToken);
        if (existingUser is not null && existingUser.Id != userId)
            throw new UsernameTakenException();

        await userRepository.UpdateUsernameAsync(userId, new UpdateUsernameDto
        {
            Username = username,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    // Changes the password of the specified user. Validates the current password.
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

    // Sets whether the specified user allows direct messages from everyone.
    public Task SetAllowDirectMessagesFromEveryoneAsync(
        Guid userId,
        bool allowDirectMessagesFromEveryone,
        CancellationToken cancellationToken = default)
    {
        return userRepository.UpdateAllowDirectMessagesFromEveryoneAsync(userId, allowDirectMessagesFromEveryone, cancellationToken);
    }

    // Deletes the account of the specified user. Validates the password.
    public async Task DeleteAccountAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        await EnsurePasswordIsCorrectAsync(userId, password, cancellationToken);
        await userRepository.DeleteUserAsync(userId, cancellationToken);
    }

    private async Task EnsurePasswordIsCorrectAsync(Guid userId, string password, CancellationToken cancellationToken)
    {
        GetUserDto user = await GetUserAsync(userId, cancellationToken);
        GetUserLoginDto? login = await userRepository.GetUserLoginAsync(user.Username, cancellationToken);

        if (login is null || string.IsNullOrEmpty(password) || !passwordHasher.Verify(password, login.PasswordHash))
            throw new IncorrectPasswordException();
    }
}
