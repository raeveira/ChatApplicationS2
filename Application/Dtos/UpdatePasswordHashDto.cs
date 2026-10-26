using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Dtos
{
    public class UpdatePasswordHashDto
    {
        public string PasswordHash { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}