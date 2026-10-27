namespace Domain
{
    // A friend request from the requester to the addressee. It becomes a friendship once the addressee accepts.
    public sealed class Friendship
    {
        public Guid RequesterId { get; }
        public Guid AddresseeId { get; }
        public EFriendshipStatus Status { get; private set; }
        public DateTime CreatedAt { get; }

        private Friendship(Guid requesterId, Guid addresseeId, EFriendshipStatus status, DateTime createdAt)
        {
            RequesterId = requesterId;
            AddresseeId = addresseeId;
            Status = status;
            CreatedAt = createdAt;
        }

        public static Friendship CreateRequest(Guid requesterId, Guid addresseeId)
        {
            if (requesterId == addresseeId)
                throw new ArgumentException("You cannot send a friend request to yourself.", nameof(addresseeId));

            return new Friendship(requesterId, addresseeId, EFriendshipStatus.Pending, DateTime.UtcNow);
        }

        // Restores a friendship from persisted data, such as a database record.
        public static Friendship Restore(Guid requesterId, Guid addresseeId, EFriendshipStatus status, DateTime createdAt)
        {
            return new Friendship(requesterId, addresseeId, status, createdAt);
        }

        public void Accept()
        {
            if (Status == EFriendshipStatus.Accepted)
                throw new InvalidOperationException("You are already friends.");

            Status = EFriendshipStatus.Accepted;
        }
    }
}
