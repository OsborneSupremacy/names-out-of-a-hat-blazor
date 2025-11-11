namespace GiftExchange.Library.Services;

internal abstract class DuplicateCheckService
{
    protected static ValidateHatResponse ValidateNoDuplicates<T>(
        IList<Person> people,
        Func<IList<Person>, IEnumerable<T>> selector,
        Func<Person, T, bool> equals
    )
    {
        var duplicateValues = new List<string>();
        var values = selector(people).ToHashSet();

        foreach (var value in values)
            if (people.Count(x => equals(x, value)) > 1)
                duplicateValues.Add(new(value?.ToString() ?? string.Empty));

        return duplicateValues.Any()
            ? new ValidateHatResponse { Success = false, Errors = duplicateValues.ToImmutableList() }
            : new ValidateHatResponse { Success = true, Errors = []};
    }

    public abstract ValidateHatResponse Execute(IList<Person> people);
}

internal class NameDuplicateCheckService : DuplicateCheckService
{
    private static readonly Func<IList<Person>, IEnumerable<string>> NameSelector = people =>
        people.Select(x => x.Name.TrimNullSafe());

    private static readonly Func<Person, string, bool> NameEquals = (person, value) =>
        person.Name.ContentEquals(value);

    public override ValidateHatResponse Execute(IList<Person> people)
    {
        var result = ValidateNoDuplicates(people, NameSelector, NameEquals);
        if (result.Success)
            return new ValidateHatResponse { Success = true, Errors =[] };

        var errors = result
            .Errors
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => $"The name, `{x}` is associated with more than one person in the gift exchange. That could cause confusion for the participants. Please differentiate between the people named `{x}` (middle/last initial, city, etc.)");

        return new ValidateHatResponse { Success = false, Errors = errors.ToImmutableList() };
    }
}

internal class EmailDuplicateCheckService : DuplicateCheckService
{
    private static readonly Func<IList<Person>, IEnumerable<string>> EmailSelector = people =>
        people.Select(x => x.Email.TrimNullSafe());

    private static readonly Func<Person, string, bool> EmailEquals = (person, value) =>
        person.Email.ContentEquals(value);

    public override ValidateHatResponse Execute(IList<Person> people)
    {
        var result = ValidateNoDuplicates(people, EmailSelector, EmailEquals);
        if (result.Success)
            return new ValidateHatResponse { Success = true, Errors =[] };

        var errors = result
            .Errors
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => $"The email address, `{x}`, is associated with more than one person in your gift exchange. If multiple names are sent to that address, that's going to cause problems. Everyone needs their own address.");

        return new ValidateHatResponse { Success = false, Errors = errors.ToImmutableList() };
    }
}

