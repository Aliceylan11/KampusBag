namespace KampusBag.Core.Entities
{
    public class CourseMembership
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid UserId { get; set; }
        public bool IsRepresentative { get; set; } // Temsilci mi?

        // Navigation Properties
        public virtual Course Course { get; set; }
        public virtual User User { get; set; }
    }
}