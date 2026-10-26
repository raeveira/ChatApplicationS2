using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain
{
    public sealed class User
    {
        Guid Id { get; init; }
        string Username { get; init; }
        string DisplayName { get; set; }
        string IconUrl { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
    }
}