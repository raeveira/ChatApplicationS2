using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class GetUserDto
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public string? DisplayName { get; set; }
        public string? IconUrl { get; set; }
        public bool AllowDirectMessagesFromEveryone { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}