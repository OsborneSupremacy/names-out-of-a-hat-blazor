using LanguageExt.Common;
using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Interface;

public interface IEmailService
{
    public Task<Result<bool>> SendAsync(EmailParts emailParts);
}
