using KampusBag.Core.Entities;
using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Helpers;

namespace KampusBag.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly IGenericRepository<Message> _messageRepo;
    private readonly IGenericRepository<EmergencyRight> _rightRepo;

    public MessageService(IGenericRepository<Message> messageRepo, IGenericRepository<EmergencyRight> rightRepo)
    {
        _messageRepo = messageRepo;
        _rightRepo = rightRepo;
    }

    // Normal Mesaj Gönderimi
    public async Task SendMessageAsync(Message message)
    {
        // 1. Şifreleme (Kilitliyoruz)
        message.Content = EncryptionHelper.Encrypt(message.Content);
        message.SentAt = DateTime.UtcNow;

        // 2. Kaydetme (Değişken ismi düzeltildi: _messageRepo)
        await _messageRepo.AddAsync(message);
    }

    public async Task<int> GetRemainingRightsAsync(Guid userId)
    {
        var rightRecord = (await _rightRepo.FindAsync(r => r.UserId == userId)).FirstOrDefault();
        return rightRecord?.RemainingRights ?? 0;
    }

    public async Task<bool> SendEmergencyMessageAsync(Guid senderId, Guid courseId, string content)
    {
        // 1. Hak kontrolü
        var rightRecord = (await _rightRepo.FindAsync(r => r.UserId == senderId)).FirstOrDefault();

        if (rightRecord == null || rightRecord.RemainingRights <= 0)
        {
            return false;
        }

        // 2. Mesajı şifreleyerek oluştur (Güvenlik eklendi!)
        var message = new Message
        {
            SenderId = senderId,
            CourseId = courseId,
            Content = EncryptionHelper.Encrypt(content), // Şifrelemeyi buraya da ekledik
            IsEmergency = true,
            SentAt = DateTime.UtcNow
        };

        await _messageRepo.AddAsync(message);

        // 3. Hakkı düş ve güncelle (Await eklendi)
        rightRecord.RemainingRights -= 1;
        await _rightRepo.UpdateAsync(rightRecord);

        return true;
    }
}