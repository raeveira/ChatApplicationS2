using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Dtos
{
    public class UpdateUserDto
    {
        public string DisplayName { get; set; }
        public string IconUrl { get; set; }
        public DateTime UpdatedAt { get; set; }
        
    }
}