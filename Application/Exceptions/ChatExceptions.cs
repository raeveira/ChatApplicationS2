
namespace Application.Exceptions;

public class ChatNotFoundException : Exception
{
    private const string DefaultMessage = "The chat does not exist.";

    public ChatNotFoundException()
        : base(DefaultMessage)
    {
    }

    public ChatNotFoundException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public ChatNotFoundException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class MessageNotFoundException : Exception
{
    private const string DefaultMessage = "The message does not exist.";

    public MessageNotFoundException()
        : base(DefaultMessage)
    {
    }

    public MessageNotFoundException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public MessageNotFoundException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class UserNotFoundException : Exception
{
    private const string DefaultMessage = "The user does not exist.";

    public UserNotFoundException()
        : base(DefaultMessage)
    {
    }

    public UserNotFoundException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public UserNotFoundException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class NotChatMemberException : Exception
{
    private const string DefaultMessage = "You are not a member of this chat.";

    public NotChatMemberException()
        : base(DefaultMessage)
    {
    }

    public NotChatMemberException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public NotChatMemberException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}

public class NotChatAdminException : Exception
{
    private const string DefaultMessage = "Only chat admins can do this.";

    public NotChatAdminException()
        : base(DefaultMessage)
    {
    }

    public NotChatAdminException(string? message)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message)
    {
    }

    public NotChatAdminException(string? message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message, innerException)
    {
    }
}
