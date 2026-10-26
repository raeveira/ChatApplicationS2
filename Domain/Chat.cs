using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain
{
    public sealed class Chat
    {
        Guid Id { get; init; }
        EChatType Type { get; init; }
        string? Name { get; set; }
        DateTime CreatedAt { get; set; }

        public Chat(EChatType type, string? name)
        {
            if (type == EChatType.Group && string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A group chat must have a name.", nameof(name));

            if (type == EChatType.Private && name != null)
                throw new ArgumentException("A private chat must not have a name.", nameof(name));

            Type = type;
            Name = name;
        }
    }
}