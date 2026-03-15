using KampusBag.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KampusBag.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; }
        public string PasswordHash { get; set; } // Şifreli hali
        public string FullName { get; set; }
        public string RegistrationNumber { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Email Doğrulama Alanları
        public bool IsEmailVerified { get; set; } = false;
        public string? VerificationCode { get; set; }
    }
}
