
namespace Application.Exceptions;

public class AlreadyFriendsException : Exception
{
    private const string DefaultMessage = "You are already friends with this user.";

    public AlreadyFriendsException()
        : base(DefaultMessage)
    {
    }

    public AlreadyFriendsException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public AlreadyFriendsException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class FriendRequestAlreadyExistsException : Exception
{
    private const string DefaultMessage = "A friend request between you and this user is already pending.";

    public FriendRequestAlreadyExistsException()
        : base(DefaultMessage)
    {
    }

    public FriendRequestAlreadyExistsException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public FriendRequestAlreadyExistsException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class FriendRequestNotFoundException : Exception
{
    private const string DefaultMessage = "The friend request does not exist.";

    public FriendRequestNotFoundException()
        : base(DefaultMessage)
    {
    }

    public FriendRequestNotFoundException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public FriendRequestNotFoundException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class NotFriendsException : Exception
{
    private const string DefaultMessage = "You can only do this with your friends.";

    public NotFriendsException()
        : base(DefaultMessage)
    {
    }

    public NotFriendsException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public NotFriendsException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class DirectMessagesNotAllowedException : Exception
{
    private const string DefaultMessage = "This user only accepts messages from friends.";

    public DirectMessagesNotAllowedException()
        : base(DefaultMessage)
    {
    }

    public DirectMessagesNotAllowedException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public DirectMessagesNotAllowedException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class IncorrectPasswordException : Exception
{
    private const string DefaultMessage = "The password is incorrect.";

    public IncorrectPasswordException()
        : base(DefaultMessage)
    {
    }

    public IncorrectPasswordException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public IncorrectPasswordException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}
