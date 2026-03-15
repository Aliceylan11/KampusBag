using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
namespace KampusBag.Core.Entities
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Her mesaj için benzersiz bir ID
        public string Content { get; set; } // Mesaj içeriği
        public DateTime SentAt { get; set; } = DateTime.UtcNow; // Mesaj gönderildiği zamanı kaydetmek için

        public Guid SenderId { get; set; } // Mesajı gönderen kullanıcı
        public virtual User Sender { get; set; } // Navigation property

        public Guid? ReceiverId { get; set; } // Birebir mesajsa
        public Guid? CourseId { get; set; }   // Grup mesajıysa
        public virtual Course Course { get; set; } // Bunu eklersen rapor çekmek kolaylaşır
        // Acil Durum Kontrolleri
        public bool IsEmergency { get; set; } // Acil durum mesajı mı?
        public bool? IsApprovedByAcademic { get; set; } // Hoca onayı
         
    }
}
