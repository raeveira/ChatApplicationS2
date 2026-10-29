using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Domain;

namespace Application.Services;

public class AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    // Registers a new user with the given request data. Validates the password and ensures the username is unique.
    public async Task<GetUserDto> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.Password);

        // The domain validates the username and display name; a blank display name falls back to the username.
        string username = User.ValidateUsername(request.Username);
        string displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username.Trim() : request.DisplayName;
        User user = User.Create(username, displayName);

        if (await userRepository.GetUserByUsernameAsync(user.Username, cancellationToken) is not null)
            throw new UsernameTakenException();

        await userRepository.CreateUserAsync(new CreateUserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            PasswordHash = passwordHasher.Hash(request.Password),
            IconUrl = user.IconUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        }, cancellationToken);

        return new GetUserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            IconUrl = user.IconUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    // Logs in a user with the given request data. Validates the credentials and returns the user information.
    public async Task<GetUserDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            throw new InvalidCredentialsException();

        GetUserLoginDto? login = await userRepository.GetUserLoginAsync(request.Username.Trim().ToLowerInvariant(), cancellationToken);

        // Unknown user and wrong password produce the same error so usernames can't be probed.
        if (login is null || !passwordHasher.Verify(request.Password, login.PasswordHash))
            throw new InvalidCredentialsException();

        return await userRepository.GetUserAsync(login.Id, cancellationToken)
            ?? throw new InvalidCredentialsException();
    }

    // Validates the given password against the defined length constraints. Throws an exception if invalid.
    public static void ValidatePassword(string password)
    {
        if (password is null || password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
        {
            throw new ArgumentException(
                $"Password must be between {MinPasswordLength} and {MaxPasswordLength} characters.", nameof(password));
        }
    }
}
