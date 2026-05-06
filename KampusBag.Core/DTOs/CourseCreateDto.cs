using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KampusBag.Core.DTOs
{

    public class CourseCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public Guid AcademicId { get; set; }
    }
}
