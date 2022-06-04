using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Utility;
using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Service;

[RegistrationTarget(typeof(IComponentModelValidationService))]
[ServiceLifetime(ServiceLifetime.Scoped)]
public class ComponentModelValidationService : IComponentModelValidationService
{
    public Result<bool> Validate<T>(T item)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator
            .TryValidateObject(item, new ValidationContext(item), results, true);
        if (!isValid)
            return new Result<bool>(new MultiException(results.Select(x => x.ErrorMessage ?? string.Empty).ToList()));
        return true;
    }

    public Result<bool> ValidateList<T>(IList<T> items)
    {
        // basic component model validation
        foreach (var item in items)
        {
            if (item == null) continue;
            var result = Validate(item);
            if (!result.IsSuccess)
                return result;
        }
        return true;
    }
}
