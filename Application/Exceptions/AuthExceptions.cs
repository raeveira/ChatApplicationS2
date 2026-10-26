namespace Application.Exceptions;

public class UsernameTakenException(string username)
    : Exception($"The username '{username}' is already taken.");

public class InvalidCredentialsException()
    : Exception("Invalid username or password.");
