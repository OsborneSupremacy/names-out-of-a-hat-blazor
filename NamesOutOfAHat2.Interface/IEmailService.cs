using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Interface
{
    public interface IEmailService
    {
        public Task<(bool success, string details)> SendAsync(EmailParts emailParts);
    }
}