namespace Application.Exceptions;

public class AlreadyFriendsException()
    : Exception("You are already friends with this user.");

public class FriendRequestAlreadyExistsException()
    : Exception("A friend request between you and this user is already pending.");

public class FriendRequestNotFoundException()
    : Exception("The friend request does not exist.");

public class NotFriendsException()
    : Exception("You can only do this with your friends.");

public class DirectMessagesNotAllowedException()
    : Exception("This user only accepts messages from friends.");

public class IncorrectPasswordException()
    : Exception("The password is incorrect.");
