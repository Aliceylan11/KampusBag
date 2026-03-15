using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KampusBag.Core.Entities
{
    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string CourseCode { get; set; }

        // Dersi veren hoca
        public Guid AcademicId { get; set; }
        public virtual User Academic { get; set; }
    }
}