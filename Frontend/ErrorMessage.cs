namespace Frontend;

public static class ErrorMessage
{
    // Turns an exception into text that is safe to show to the user.
    public static string For(Exception exception)
    {
        if (exception is ArgumentException)
        {
            int parameterIndex = exception.Message.IndexOf(" (Parameter", StringComparison.Ordinal);
            return parameterIndex >= 0 ? exception.Message[..parameterIndex] : exception.Message;
        }

        bool isOurOwnException = exception is InvalidOperationException
                                || exception.GetType().Namespace == "Application.Exceptions";

        return isOurOwnException ? exception.Message : "Something went wrong. Please try again.";
    }
}
