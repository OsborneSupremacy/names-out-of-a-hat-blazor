using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Utility;
using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Service
{
    [RegistrationTarget(typeof(IComponentModelValidationService))]
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ComponentModelValidationService : IComponentModelValidationService
    {
        public (bool isValid, IList<string> errors) Validate<T>(T item)
        {
            var results = new List<ValidationResult>();
            var isValid = Validator
                .TryValidateObject(item, new ValidationContext(item), results, true);
            if (!isValid)
                return (false, results.Select(x => x.ErrorMessage ?? string.Empty).ToList());
            return (true, Enumerable.Empty<string>().ToList());
        }

        public (bool isValid, IList<string> errors) ValidateList<T>(IList<T> items)
        {
            // basic component model validation
            foreach (var item in items)
            {
                if (item == null) continue;
                var (isValid, errors) = Validate(item);
                if (!isValid)
                    return (false, errors);
            }
            return (true, Enumerable.Empty<string>().ToList());
        }
    }
}
