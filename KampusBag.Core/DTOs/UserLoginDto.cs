using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KampusBag.Core.DTOs
{
    public class UserLoginDto
    {
        public string Identifier { get; set; } // Email veya RegistrationNumber
        public string Password { get; set; }
    }
}
