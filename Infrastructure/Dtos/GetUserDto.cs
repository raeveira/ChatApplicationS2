using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Dtos
{
    public class GetUserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string? DisplayName { get; set; }
        public string? IconUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}