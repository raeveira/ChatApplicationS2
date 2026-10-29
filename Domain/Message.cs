namespace Domain;

public sealed class Message
{
    public const int MaxContentLength = 4000;

    public Guid Id { get; }
    public Guid SentById { get; }
    public Guid SentInId { get; }
    public string Content { get; }
    public DateTime SentAt { get; }

    private Message(Guid id, Guid sentById, Guid sentInId, string content, DateTime sentAt)
    {
        Id = id;
        SentById = sentById;
        SentInId = sentInId;
        Content = content;
        SentAt = sentAt;
    }

    public static Message Create(Guid sentById, Guid sentInId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty.", nameof(content));

        var trimmed = content.Trim();
        if (trimmed.Length > MaxContentLength)
            throw new ArgumentException(
                $"Message content cannot be longer than {MaxContentLength} characters.", nameof(content));

        return new Message(Guid.NewGuid(), sentById, sentInId, trimmed, DateTime.UtcNow);
    }

    // Rebuilds a message from stored data; the values were validated when it was first sent.
    public static Message Restore(Guid id, Guid sentById, Guid sentInId, string content, DateTime sentAt)
    {
        return new Message(id, sentById, sentInId, content, sentAt);
    }
}
