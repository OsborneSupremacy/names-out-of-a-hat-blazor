using LanguageExt.Common;

namespace NamesOutOfAHat2.Interface;

public interface IComponentModelValidationService
{
    public Result<bool> Validate<T>(T item);

    public Result<bool> ValidateList<T>(IList<T> items);
}
