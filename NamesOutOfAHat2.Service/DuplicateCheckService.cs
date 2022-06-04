using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service;

public abstract class DuplicateCheckService
{
    protected static Result<bool> ValidateNoDuplicates<T>(
        IList<Person> people,
        Func<IList<Person>, IEnumerable<T>> selector,
        Func<Person, T, bool> equals
    )
    {
        var duplicateValues = new List<string>();
        var values = selector(people).ToHashSet();

        foreach (var value in values)
            if (people
                .Where(x => equals(x, value))
                .Count() > 1)
                duplicateValues.Add(value?.ToString() ?? string.Empty);

        if (duplicateValues.Any())
            return new Result<bool>(new MultiException(duplicateValues));

        return true;
    }
}

[RegistrationTarget(typeof(IDuplicateCheckService))]
[ServiceLifetime(ServiceLifetime.Scoped)]
public class NameDuplicateCheckService : DuplicateCheckService, IDuplicateCheckService
{
    protected readonly static Func<IList<Person>, IEnumerable<string>> _nameSelector = (IList<Person> people) =>
        people.Select(x => x.Name.TrimNullSafe());

    protected readonly static Func<Person, string, bool> _nameEquals = (Person person, string value) =>
        person.Name.ContentEquals(value);

    public Result<bool> Execute(IList<Person> people)
    {
        var result = ValidateNoDuplicates(people, _nameSelector, _nameEquals);
        if (result.IsSuccess) return true;

        var errors = result
            .GetErrors()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => $"The name, `{x}` is associated with more than one person in your gift exchange. That could cause confusion for the participants. Please differentiate between the people named `{x}` (middle/last initial, city, etc.)");

        return new Result<bool>(new MultiException(errors.ToList()));
    }
}

[RegistrationTarget(typeof(IDuplicateCheckService))]
[ServiceLifetime(ServiceLifetime.Scoped)]
public class EmailDuplicateCheckService : DuplicateCheckService, IDuplicateCheckService
{
    protected readonly static Func<IList<Person>, IEnumerable<string>> _emailSelector = (IList<Person> people) =>
        people.Select(x => x.Email.TrimNullSafe());

    protected readonly static Func<Person, string, bool> _emailEquals = (Person person, string value) =>
        person.Email.ContentEquals(value);

    public Result<bool> Execute(IList<Person> people)
    {
        var result = ValidateNoDuplicates(people, _emailSelector, _emailEquals);
        if (result.IsSuccess) return true;

        var errors = result
            .GetErrors()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => $"The email address, `{x}`, is associated with more than one person in your gift exchange. If multiple names are sent to that address, that's going to cause problems. Everyone needs their own address.");

        return new Result<bool>(new MultiException(errors.ToList()));
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
    protected readonly static Func<IList<Person>, IEnumerable<Guid>> _idSelector = (IList<Person> people) =>
        people.Select(x => x.Id);

    protected readonly static Func<Person, Guid, bool> _idEquals = (Person person, Guid value) =>
        person.Id.Equals(value);

    public Result<bool> Execute(IList<Person> people)
    {
        var result = ValidateNoDuplicates(people, _idSelector, _idEquals);
        if (result.IsSuccess) return result;

        return new Result<bool>(new MultiException("We apologize but there in an internal issue with this application."));
    }
}
