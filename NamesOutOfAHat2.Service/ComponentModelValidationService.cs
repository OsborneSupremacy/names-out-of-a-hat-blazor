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
        {
            var exceptions = results.Select(x => new ValidationException(x.ErrorMessage ?? string.Empty));
            return new Result<bool>(new AggregateException(exceptions));
        }

        return true;
    }

    public Result<bool> ValidateList<T>(IList<T> items)
    {
        // TODO: replace this with FluentValidation
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
