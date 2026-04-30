namespace KampusBag.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendVerificationCodeAsync(string email, string code);
    Task<bool> SendPasswordResetCodeAsync(string email, string code);
}