using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Service;

public abstract class DuplicateCheckService
{
    protected static Result<bool> ValidateNoDuplicates<T>(
        IList<Person> people,
        Func<IList<Person>, IEnumerable<T>> selector,
        Func<Person, T, bool> equals
    )
    {
        var duplicateValues = new List<ValidationException>();
        var values = selector(people).ToHashSet();

        foreach (var value in values)
            if (people
                .Where(x => equals(x, value))
                .Count() > 1)
                duplicateValues.Add(new(value?.ToString() ?? string.Empty));

        if (duplicateValues.Any())
            return new Result<bool>(new AggregateException(duplicateValues));

        return true;
    }
}

[RegistrationTarget(typeof(IDuplicateCheckService))]
[ServiceLifetime(ServiceLifetime.Scoped)]
public class NameDuplicateCheckService : DuplicateCheckService, IDuplicateCheckService
{
    protected static readonly Func<IList<Person>, IEnumerable<string>> NameSelector = (IList<Person> people) =>
        people.Select(x => x.Name.TrimNullSafe());

    protected static readonly Func<Person, string, bool> NameEquals = (Person person, string value) =>
        person.Name.ContentEquals(value);

    public Result<bool> Execute(IList<Person> people)
    {
        var result = ValidateNoDuplicates(people, NameSelector, NameEquals);
        if (result.IsSuccess) return true;

        var errors = result
            .GetErrors()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(
                x => new ValidationException($"The name, `{x}` is associated with more than one person in your gift exchange. That could cause confusion for the participants. Please differentiate between the people named `{x}` (middle/last initial, city, etc.)")
            );

        return new Result<bool>(new AggregateException(errors));
    }
}

[RegistrationTarget(typeof(IDuplicateCheckService))]
[ServiceLifetime(ServiceLifetime.Scoped)]
public class EmailDuplicateCheckService : DuplicateCheckService, IDuplicateCheckService
{
    private static readonly Func<IList<Person>, IEnumerable<string>> EmailSelector = (IList<Person> people) =>
        people.Select(x => x.Email.TrimNullSafe());

    private static readonly Func<Person, string, bool> EmailEquals = (Person person, string value) =>
        person.Email.ContentEquals(value);

    public Result<bool> Execute(IList<Person> people)
    {
        var result = ValidateNoDuplicates(people, EmailSelector, EmailEquals);
        if (result.IsSuccess) return true;

        var errors = result
            .GetErrors()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => 
                new ValidationException($"The email address, `{x}`, is associated with more than one person in your gift exchange. If multiple names are sent to that address, that's going to cause problems. Everyone needs their own address.")
            );

        return new Result<bool>(new AggregateException(errors));
    }
}

/// <summary>
/// IDs are assigned on the frontend. We want to verify that no ID is re-used. This would only
/// happen if there was a bug on the frontend. Probably not likely, but could yield bad consequences
/// (e.g. a person being assigned to themself)
/// </summary>
[RegistrationTarget(typeof(IDuplicateCheckService))]
[ServiceLifetime(ServiceLifetime.Scoped)]
public class IdDuplicateCheckService : DuplicateCheckService, IDuplicateCheckService
{
    private static readonly Func<IList<Person>, IEnumerable<Guid>> IdSelector = (IList<Person> people) =>
        people.Select(x => x.Id);

    private static readonly Func<Person, Guid, bool> IdEquals = (Person person, Guid value) =>
        person.Id.Equals(value);

    public Result<bool> Execute(IList<Person> people) =>
        ValidateNoDuplicates(people, IdSelector, IdEquals);
}
