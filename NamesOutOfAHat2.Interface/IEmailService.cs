using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Interface
{
    public interface IEmailService
    {
        public Task SendAsync(EmailParts emailParts);
    }
}
