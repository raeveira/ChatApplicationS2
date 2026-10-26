namespace Domain
{
    public sealed class User
    {
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 32;
        public const int MaxDisplayNameLength = 64;

        public Guid Id { get; }
        public string Username { get; }
        public string DisplayName { get; private set; }
        public string? IconUrl { get; private set; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; private set; }

        private User(Guid id, string username, string displayName, string? iconUrl, DateTime createdAt, DateTime updatedAt)
        {
            Id = id;
            Username = username;
            DisplayName = displayName;
            IconUrl = iconUrl;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static User Create(string username, string displayName, string? iconUrl = null)
        {
            var now = DateTime.UtcNow;
            return new User(
                Guid.NewGuid(),
                ValidateUsername(username),
                ValidateDisplayName(displayName),
                NormalizeIconUrl(iconUrl),
                now,
                now);
        }

        // Rebuilds a user from stored data; the values were validated when they were first saved.
        public static User Restore(Guid id, string username, string displayName, string? iconUrl, DateTime createdAt, DateTime updatedAt)
        {
            return new User(id, username, displayName, iconUrl, createdAt, updatedAt);
        }

        public void UpdateProfile(string displayName, string? iconUrl)
        {
            DisplayName = ValidateDisplayName(displayName);
            IconUrl = NormalizeIconUrl(iconUrl);
            UpdatedAt = DateTime.UtcNow;
        }

        public static string ValidateUsername(string username)
        {
            var trimmed = username?.Trim() ?? string.Empty;

            if (trimmed.Length < MinUsernameLength || trimmed.Length > MaxUsernameLength)
                throw new ArgumentException(
                    $"Username must be between {MinUsernameLength} and {MaxUsernameLength} characters.", nameof(username));

            if (!trimmed.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
                throw new ArgumentException(
                    "Username may only contain letters, digits and underscores.", nameof(username));

            return trimmed.ToLowerInvariant();
        }

        public static string ValidateDisplayName(string displayName)
        {
            var trimmed = displayName?.Trim() ?? string.Empty;

            if (trimmed.Length == 0 || trimmed.Length > MaxDisplayNameLength)
                throw new ArgumentException(
                    $"Display name must be between 1 and {MaxDisplayNameLength} characters.", nameof(displayName));

            return trimmed;
        }

        private static string? NormalizeIconUrl(string? iconUrl)
        {
            return string.IsNullOrWhiteSpace(iconUrl) ? null : iconUrl.Trim();
        }
    }
}