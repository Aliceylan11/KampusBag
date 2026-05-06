using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KampusBag.MobileUI.Models
{

    public class UserProfileModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public int Role { get; set; }

        // İstatistikler (Backend'den Count(*) ile gelecek)
        public int TotalCourses { get; set; }
        public int TotalMessages { get; set; }
        public int RemainingEmergencyRights { get; set; }
    }
}
