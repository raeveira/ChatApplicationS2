using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain
{
    public sealed class Message
    {
        Guid Id { get; init; }
        Guid SentById { get; init; }
        Guid SentInId { get; init; }
        string Content { get; set; }
        DateTime SentAt { get; init; }

        public Message(string content)
        {
            if(string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.", nameof(content));

            Content = content;
        }
    }
}