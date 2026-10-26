using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Dtos
{
    public class UpdateUsernameDto
    {
        public string Username { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}