using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class UpdateUsernameDto
    {
        public required string Username { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}