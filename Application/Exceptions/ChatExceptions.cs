namespace Application.Exceptions;

public class ChatNotFoundException()
    : Exception("The chat does not exist.");

public class MessageNotFoundException()
    : Exception("The message does not exist.");

public class UserNotFoundException()
    : Exception("The user does not exist.");

public class NotChatMemberException()
    : Exception("You are not a member of this chat.");

public class NotChatAdminException()
    : Exception("Only chat admins can do this.");
