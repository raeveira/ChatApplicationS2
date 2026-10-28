namespace Application.Exceptions;

public class UsernameTakenException : Exception
{
    private const string DefaultMessage = "The username is already taken.";

    public UsernameTakenException()
        : base(DefaultMessage)
    {
    }

    public UsernameTakenException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public UsernameTakenException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class InvalidCredentialsException : Exception
{
    private const string DefaultMessage = "Invalid username or password.";

    public InvalidCredentialsException()
        : base(DefaultMessage)
    {
    }

    public InvalidCredentialsException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public InvalidCredentialsException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}