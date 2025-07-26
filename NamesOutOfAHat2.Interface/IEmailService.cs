using LanguageExt.Common;
using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Interface;

public interface IEmailService
{
    public Task<Result<bool>> SendAsync(Invitation invitation);
}
