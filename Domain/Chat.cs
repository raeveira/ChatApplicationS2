namespace Domain;

public sealed class Chat
{
    public Guid Id { get; }
    public EChatType Type { get; }
    public string? Name { get; private set; }
    public DateTime CreatedAt { get; }

    private Chat(Guid id, EChatType type, string? name, DateTime createdAt)
    {
        Id = id;
        Type = type;
        Name = name;
        CreatedAt = createdAt;
    }

    public static Chat Create(EChatType type, string? name)
    {
        return new Chat(Guid.NewGuid(), type, ValidateName(type, name), DateTime.UtcNow);
    }

    // Rebuilds a chat from stored data; the values were validated when they were first saved.
    public static Chat Restore(Guid id, EChatType type, string? name, DateTime createdAt)
    {
        return new Chat(id, type, name, createdAt);
    }

    public void Rename(string? name)
    {
        Name = ValidateName(Type, name);
    }

    private static string? ValidateName(EChatType type, string? name)
    {
        if (type == EChatType.Group)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A group chat must have a name.", nameof(name));

            return name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A private chat must not have a name.", nameof(name));

        return null;
    }
}
