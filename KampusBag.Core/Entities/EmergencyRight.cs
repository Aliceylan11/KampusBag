using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KampusBag.Core.Entities
{
    public class EmergencyRight
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; } // Hangi öğrenciye ait?
        public int RemainingRights { get; set; } = 3; // Dönemlik 3 hak
        public string AcademicTerm { get; set; } // Örn: "2025-2026-Guz"

        // Navigation Property
        public virtual User User { get; set; }
    }
}
