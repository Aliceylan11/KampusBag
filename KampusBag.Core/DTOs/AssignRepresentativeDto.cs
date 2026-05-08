namespace KampusBag.Core.DTOs;

/// <summary>
/// Hoca tarafından öğrenciye temsilcilik atama / geri alma isteği.
/// JWT entegre edilene kadar AcademicId body'den alınır ve sunucuda doğrulanır.
/// </summary>
public class AssignRepresentativeDto
{
    /// <summary>İşlemi yapan akademisyenin kimliği (sunucuda Course.AcademicId ile doğrulanır).</summary>
    public Guid AcademicId { get; set; }

    /// <summary>Temsilci yapılacak veya temsilciliği alınacak öğrencinin kimliği.</summary>
    public Guid StudentId { get; set; }

    /// <summary>true = temsilciliği geri al, false (varsayılan) = temsilci ata.</summary>
    public bool Revoke { get; set; } = false;
}
